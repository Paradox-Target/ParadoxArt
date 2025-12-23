using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
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
using Microsoft.Extensions.DependencyInjection;
using NLog;
using ZLinq;

namespace Hoi4BlueprintBuilder.Core.Views;

[RegisterTransient<EditorCanvasView>]
public sealed partial class EditorCanvasView : UserControl, ITabViewItem, IClosed
{
    public string Header { get; }
    public string FilePath { get; }
    public string ToolTip { get; }

    private readonly EditorCanvasViewModel _viewModel;
    private readonly ScreenshotService _screenshotService;
    private readonly LocalizationFormatService _localizationFormatService;
    private readonly FAMenuFlyout _menuFlyout;
    private readonly MessageBoxService _messageBox;
    private readonly FileService _fileService;
    private CanvasInteractionManager? _interactionManager;

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

    public EditorCanvasView(
        EditorCanvasViewModel viewModel,
        ScreenshotService screenshotService,
        MessageBoxService messageBox,
        LocalizationFormatService localizationFormatService,
        FileService fileService,
        UserStatusService userStatusService
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

        _viewModel = viewModel;
        _screenshotService = screenshotService;
        _messageBox = messageBox;
        _localizationFormatService = localizationFormatService;
        _fileService = fileService;
        DataContext = _viewModel;
        if (userStatusService.CurrentSelectedFile is null)
        {
            throw new InvalidOperationException("当前没有选中的国策文件");
        }
        _viewModel.OpenFile(userStatusService.CurrentSelectedFile.FullPath);

        FilePath = userStatusService.CurrentSelectedFile.FullPath;
        Header = userStatusService.CurrentSelectedFile.Name;
        ToolTip = userStatusService.CurrentSelectedFile.FullPath;
    }

    private void OnUnloaded(object? sender, RoutedEventArgs e)
    {
        WeakReferenceMessenger.Default.UnregisterAll(this);
        _viewModel.OnUnLoaded();
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        WeakReferenceMessenger.Default.Register<SaveFocusTreeToPngMessage>(this, SaveToPng);
        _viewModel.OnLoaded();
    }

    private void OnConnectionRequested(FocusNode from, FocusNode to, ConnectionType type)
    {
        _viewModel.CreateConnection(from, to, type);
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
            _viewModel,
            this,
            ConnectionPreviewOverlay,
            OpenFocusInfoViewInternal,
            CloseFocusInfoView
        );

        _interactionManager.ConnectionRequested += OnConnectionRequested;
    }

    private async void SaveToPng(object o, SaveFocusTreeToPngMessage saveFocusTreeToPngMessage)
    {
        var nodes = _viewModel.Nodes;
        if (nodes.Count == 0)
        {
            await _messageBox.ShowAsync("没有可显示的国策", "错误", MessageBoxIcon.Error);
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
            await _messageBox.ShowAsync("导出图片失败", "错误", MessageBoxIcon.Error);
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
        Cursor = new Cursor(cursorType);
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

        _viewModel.DeleteFocusNode(focus);

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
        var viewModel = new CreateNewFocusViewModel(_viewModel.GetAllFocusFiles())
        {
            FocusId = _viewModel.GetNextFocusId()
        };
        var dialog = new ContentDialog
        {
            Title = "新建国策",
            Content = new CreateNewFocusView { DataContext = viewModel },
            CloseButtonText = "取消",
            PrimaryButtonText = "创建",
            DefaultButton = ContentDialogButton.Primary,
            IsPrimaryButtonEnabled = false
        };

        Action<bool> onPrimaryEnableChanged = enable => dialog.IsPrimaryButtonEnabled = enable;
        viewModel.PrimaryEnableChanged += onPrimaryEnableChanged;
        var mainWindow = GetMainWindow();
        if (mainWindow is null)
        {
            return;
        }
        var result = await dialog.ShowAsync(mainWindow);

        if (result == ContentDialogResult.Primary)
        {
            var position = _interactionManager?.GetRightClickGridPosition() ?? (0, 0);
            var newFocusNode = await WeakReferenceMessenger.Default.Send(
                new CreateNewFocusMessage(
                    new FocusPoint(position.X, position.Y),
                    viewModel.FocusId,
                    viewModel.SelectedFocusType,
                    viewModel.SelectedFocusFilePath
                )
            );

            OpenFocusInfoView(newFocusNode);
            Log.Debug("创建新国策: {Name}", newFocusNode.Id);
        }
        viewModel.PrimaryEnableChanged -= onPrimaryEnableChanged;
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

    private static Window? GetMainWindow()
    {
        return
            App.Current.ApplicationLifetime
                is IClassicDesktopStyleApplicationLifetime { MainWindow: not null } desktop
            ? desktop.MainWindow
            : null;
    }

    public void Close()
    {
        _viewModel.Close();
    }
}
