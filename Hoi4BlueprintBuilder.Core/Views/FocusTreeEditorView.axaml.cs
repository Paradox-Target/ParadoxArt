using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FluentAvalonia.UI.Controls;
using Hoi4BlueprintBuilder.Core.Messages;
using Hoi4BlueprintBuilder.Core.Models;
using Hoi4BlueprintBuilder.Core.Models.Focus;
using Hoi4BlueprintBuilder.Core.Services;
using Hoi4BlueprintBuilder.Core.Services.GameResources.Localization;
using Hoi4BlueprintBuilder.Core.Views.Dialogs;
using Hoi4BlueprintBuilder.Core.ViewsModels;
using Hoi4BlueprintBuilder.Core.ViewsModels.Dialogs;
using Hoi4BlueprintBuilder.Localization.Strings;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using ZLinq;

namespace Hoi4BlueprintBuilder.Core.Views;

[RegisterTransient<FocusTreeEditorView>]
public sealed partial class FocusTreeEditorView : UserControl, ITabViewItem, IClosed, ISave
{
    public string Header { get; }
    public string FilePath { get; }
    public string ToolTip { get; }

    public FocusTreeEditorViewModel ViewModel { get; }

    private readonly ScreenshotService _screenshotService;
    private readonly LocalizationFormatService _localizationFormatService;
    private readonly FAMenuFlyout _menuFlyout;
    private readonly MessageBoxService _messageBox;
    private readonly FileService _fileService;
    private readonly SettingsService _settingsService;
    private CanvasInteractionManager? _interactionManager;
    private readonly Dictionary<StandardCursorType, Cursor> _cursorCache = new();

    private const double FocusInfoViewWidthRatio = 0.35;
    private const double FocusInfoViewHeightRatio = 0.9;
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// 鼠标右键点击位置是否在某个国策节点上
    /// </summary>
    private bool CursorOverFocus => _interactionManager?.CursorOverFocus ?? false;

    /// <summary>
    /// 鼠标右键点击位置是否不在某个国策节点上
    /// </summary>
    private bool CursorNotOverFocus => !CursorOverFocus;

    /// <summary>
    /// 是否可以转换为绝对位置
    /// </summary>
    private bool CanConvertToAbsolutePosition =>
        _interactionManager?.RightClickedNode?.RelativePosition is not null;

    private bool _isFirstLoaded;

    public FocusTreeEditorView(
        FocusTreeEditorViewModel viewModel,
        ScreenshotService screenshotService,
        MessageBoxService messageBox,
        LocalizationFormatService localizationFormatService,
        FileService fileService,
        UserStatusService userStatusService,
        SettingsService settingsService
    )
    {
        InitializeComponent();

        _menuFlyout = (FAMenuFlyout?)MainGrid.ContextFlyout ?? throw new ArgumentNullException();
        PointerWheelChanged += OnPointerWheelChanged;
        PointerMoved += OnPointerMoved;
        PointerExited += OnPointerExited;
        PointerPressed += OnPointerPressed;
        AddHandler(PointerReleasedEvent, OnPointerReleased, RoutingStrategies.Tunnel);
        Initialized += OnInitialized;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;

        ViewModel = viewModel;
        _screenshotService = screenshotService;
        _messageBox = messageBox;
        _localizationFormatService = localizationFormatService;
        _fileService = fileService;
        _settingsService = settingsService;
        DataContext = ViewModel;
        if (userStatusService.CurrentSelectedFile is null)
        {
            throw new InvalidOperationException("当前没有选中的国策文件");
        }

        FilePath = userStatusService.CurrentSelectedFile.FullPath;
        Header = userStatusService.CurrentSelectedFile.Name;
        ToolTip = userStatusService.CurrentSelectedFile.FullPath;
    }

    private void OnUnloaded(object? sender, RoutedEventArgs e)
    {
        StrongReferenceMessenger.Default.UnregisterAll(this);
        ViewModel.OnUnLoaded();
    }

    private async void OnLoaded(object? sender, RoutedEventArgs e)
    {
        StrongReferenceMessenger.Default.Register<SaveFocusTreeToPngMessage>(this, SaveToPng);
        ViewModel.OnLoaded();
        if (!_isFirstLoaded)
        {
            await ViewModel.LoadFocusTreeFileAsync(FilePath);
        }
        _isFirstLoaded = true;
    }

    private void OnConnectionRequested(FocusNode from, FocusNode to, ConnectionType type)
    {
        ViewModel.CreateConnection(from, to, type);
    }

    private bool OpenFocusInfoViewInternal(FocusNode focusNode)
    {
        OpenFocusInfoView(focusNode);
        return true;
    }

    private void CloseFocusInfoView()
    {
        FocusInfoViewControl.IsOpen = false;
    }

    private void OnInitialized(object? sender, EventArgs e)
    {
        // 方便右键菜单在前端绑定 Command
        foreach (MenuFlyoutItem? item in _menuFlyout.Items)
        {
            item?.DataContext = this;
        }

        // 控件加载完成后初始化交互管理器
        _interactionManager = new CanvasInteractionManager(
            ViewModel,
            this,
            ConnectionPreviewOverlay,
            OpenFocusInfoViewInternal,
            CloseFocusInfoView
        );

        _interactionManager.ConnectionRequested += OnConnectionRequested;
    }

    private async void SaveToPng(object o, SaveFocusTreeToPngMessage saveFocusTreeToPngMessage)
    {
        var nodes = ViewModel.Nodes;
        if (nodes.Count == 0)
        {
            await _messageBox.ShowAsync("没有可显示的国策", LangResources.Common_Error, MessageBoxIcon.Error);
            return;
        }

        var file = await _fileService.SaveFileAsync(
            new FilePickerSaveOptions
            {
                Title = "导出国策树为图片",
                DefaultExtension = ".png",
                SuggestedFileName = "FocusTree.png",
                FileTypeChoices =
                [
                    new FilePickerFileType("PNG 图片") { Patterns = ["*.png"] },
                    new FilePickerFileType("JPG 图片") { Patterns = ["*.jpg", "*.jpeg"] },
                    new FilePickerFileType("BMP 图片") { Patterns = ["*.bmp"] }
                ]
            }
        );

        if (file is null)
        {
            return;
        }

        var notificationService = App.Current.Services.GetRequiredService<NotificationService>();
        try
        {
            _screenshotService.SaveFocusTreeScreenshot(nodes, file.Path.LocalPath);
            Log.Info("已导出图片: {FileName}", file.Path.LocalPath);
            notificationService.Show("导出图片成功");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "导出国策树图片失败");
            await _messageBox.ShowAsync("导出图片失败", LangResources.Common_Error, MessageBoxIcon.Error);
        }
    }

    #region 鼠标事件处理

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        _interactionManager?.HandlePointerPressed(e);
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_interactionManager is null)
        {
            return;
        }

        // 在释放前检查是否应该显示上下文菜单
        bool shouldShowMenu = _interactionManager.ShouldShowContextMenu();
        var props = e.GetCurrentPoint(this).Properties;

        _interactionManager.HandlePointerReleased(e);

        // 右键释放时，如果应该显示菜单则显示
        if (shouldShowMenu && props.PointerUpdateKind == PointerUpdateKind.RightButtonReleased)
        {
            // 右键打开菜单时，先清除已有选择，再选中被指向的节点以便展示
            _interactionManager.ClearSelection();

            var focus = _interactionManager?.RightClickedNodeViewModel;
            focus?.IsSelected = true;

            _menuFlyout.ShowAt(this, true);
        }
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_interactionManager is null)
        {
            return;
        }

        var cursorType = _interactionManager.HandlePointerMoved(e);
        if (!_cursorCache.TryGetValue(cursorType, out var cursor))
        {
            cursor = new Cursor(cursorType);
            _cursorCache.Add(cursorType, cursor);
        }

        Cursor = cursor;
    }

    private void OnPointerExited(object? sender, PointerEventArgs e)
    {
        _interactionManager?.HandlePointerExited();
        Cursor = Cursor.Default;
    }

    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        _interactionManager?.HandlePointerWheelChanged(e);
    }

    #endregion

    #region 上下文菜单命令

    [RelayCommand(CanExecute = nameof(CursorOverFocus))]
    private void SetMutuallyExclusiveFocus()
    {
        var focusNode = _interactionManager?.RightClickedNodeViewModel;
        if (focusNode is null)
        {
            return;
        }

        _interactionManager?.StartConnection(focusNode, ConnectionType.MutuallyExclusive);
    }

    [RelayCommand(CanExecute = nameof(CursorOverFocus))]
    private void SetPrerequisiteFocus()
    {
        var focusNode = _interactionManager?.RightClickedNodeViewModel;
        if (focusNode is null)
        {
            return;
        }

        _interactionManager?.StartConnection(focusNode, ConnectionType.Prerequisite);
    }

    [RelayCommand(CanExecute = nameof(CursorOverFocus))]
    private async Task DeleteFocusNode()
    {
        var focus = _interactionManager?.RightClickedNode;
        if (focus is null)
        {
            return;
        }

        if (focus.RelativePositionChildren.Count > 0)
        {
            string impactedFocusIds = focus
                .RelativePositionChildren.AsValueEnumerable()
                .Select(f => _localizationFormatService.GetFormatText(f.Id))
                .JoinToString('\n');
            var result = await _messageBox.ShowAsync(
                $"有其他国策使用这个国策的相对位置, 删除后会导致这些国策的位置变更为绝对位置, 是否确认删除?\n\n受影响节点:\n\n{impactedFocusIds}",
                "确认删除",
                MessageBoxIcon.Warning,
                MessageBoxButtons.YesNo
            );
            if (result != MessageBoxResult.Yes)
            {
                return;
            }
        }

        // 关闭信息卡, 并释放 ViewModel 资源, 防止内存泄漏
        if (
            FocusInfoViewControl.DataContext is FocusInfoViewModel infoViewModel
            && infoViewModel.FocusNode == focus
        )
        {
            FocusInfoViewControl.IsOpen = false;
            FocusInfoViewControl.DataContext = null;
        }

        ViewModel.DeleteFocusNode(focus);

        Debug.Assert(ConnectionPreviewOverlay.From != focus && ConnectionPreviewOverlay.To != focus);
    }

    [RelayCommand(CanExecute = nameof(CursorOverFocus))]
    private void SetRelativePosition()
    {
        var focusNode = _interactionManager?.RightClickedNodeViewModel;
        if (focusNode is null)
        {
            return;
        }

        _interactionManager?.StartConnection(focusNode, ConnectionType.RelativePosition);
    }

    [RelayCommand(CanExecute = nameof(CursorNotOverFocus))]
    private async Task CreateNewFocus()
    {
        var dialog = new ContentDialog
        {
            Title = "新建国策",
            CloseButtonText = "取消",
            PrimaryButtonText = "创建",
            DefaultButton = ContentDialogButton.Primary,
            IsPrimaryButtonEnabled = false
        };
        var viewModel = new CreateNewFocusViewModel(
            ViewModel.GetAllFocusFiles(),
            enable => dialog.IsPrimaryButtonEnabled = enable,
            focusId => ViewModel.ContainsFocus(focusId)
        )
        {
            FocusId = ViewModel.GetNextFocusId()
        };
        var content = new CreateNewFocusView { DataContext = viewModel };
        dialog.Content = content;

        var result = await dialog.ShowAsync();
        viewModel.Clean();

        if (result == ContentDialogResult.Primary)
        {
            var position = _interactionManager?.GetRightClickGridPosition() ?? (0, 0);
            var newFocusNode = await StrongReferenceMessenger.Default.Send(
                new CreateNewFocusMessage(
                    new FocusPoint(position.X, position.Y),
                    viewModel.FocusId,
                    viewModel.SelectedFocusType,
                    viewModel.SelectedFocusFilePath
                )
            );

            if (_settingsService.IsAutoOpenFocusInfoCard)
            {
                OpenFocusInfoView(newFocusNode);
            }
            Log.Debug("创建新国策: {Name}", newFocusNode.Id);
        }
    }

    [RelayCommand(CanExecute = nameof(CanConvertToAbsolutePosition))]
    private void ConvertToAbsolutePosition()
    {
        _interactionManager?.RightClickedNode?.ConvertToAbsolutePosition();
    }

    private void ContextMenu_OnOpening(object sender, EventArgs e)
    {
        CreateNewFocusCommand.NotifyCanExecuteChanged();
        SetPrerequisiteFocusCommand.NotifyCanExecuteChanged();
        SetMutuallyExclusiveFocusCommand.NotifyCanExecuteChanged();
        DeleteFocusNodeCommand.NotifyCanExecuteChanged();
        ConvertToAbsolutePositionCommand.NotifyCanExecuteChanged();
        SetRelativePositionCommand.NotifyCanExecuteChanged();
    }

    #endregion

    private void OpenFocusInfoView(FocusNode focusNode)
    {
        FocusInfoViewControl.DataContext = new FocusInfoViewModel(focusNode);
        FocusInfoViewControl.Width = Bounds.Width * FocusInfoViewWidthRatio;
        FocusInfoViewControl.Height = Bounds.Height * FocusInfoViewHeightRatio;
        FocusInfoViewControl.IsOpen = true;
    }

    public void Close()
    {
        ViewModel.Close();
    }

    public void Save()
    {
        ViewModel.SaveFocusTree();
    }
}
