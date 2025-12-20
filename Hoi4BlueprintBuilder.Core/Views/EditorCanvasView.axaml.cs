using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FluentAvalonia.UI.Controls;
using Hoi4BlueprintBuilder.Core.Constants;
using Hoi4BlueprintBuilder.Core.Messages;
using Hoi4BlueprintBuilder.Core.Models;
using Hoi4BlueprintBuilder.Core.Models.Focus;
using Hoi4BlueprintBuilder.Core.Services;
using Hoi4BlueprintBuilder.Core.Services.GameResources.Localization;
using Hoi4BlueprintBuilder.Core.Views.Dialogs;
using Hoi4BlueprintBuilder.Core.ViewsModels;
using Hoi4BlueprintBuilder.Core.ViewsModels.Dialogs;
using Microsoft.Extensions.DependencyInjection;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using NLog;
using ZLinq;

namespace Hoi4BlueprintBuilder.Core.Views;

[RegisterSingleton<EditorCanvasView>]
public sealed partial class EditorCanvasView : UserControl
{
    private Point? _lastMousePositionOnCanvas;
    private FocusNode? _movedFocusNode;
    private readonly EditorCanvasViewModel _viewModel;
    private FocusNode? _lastRightClickFocus;
    private Point _lastRightClickPoint;
    private readonly ScreenshotService _screenshotService;
    private readonly LocalizationFormatService _localizationFormatService =
        App.Current.Services.GetRequiredService<LocalizationFormatService>();

    private ConnectionType FocusConnectionMode
    {
        get;
        set
        {
            field = value;
            ConnectionPreviewOverlay.Mode = value;
        }
    } = ConnectionType.None;

    /// <summary>
    /// 鼠标右键点击位置是否在某个国策节点上
    /// </summary>
    private bool CursorOverFocus => _lastRightClickFocus is not null;

    /// <summary>
    /// 鼠标右键点击位置是否不在某个国策节点上
    /// </summary>
    private bool CursorNotOverFocus => !CursorOverFocus;

    private MenuFlyout _menuFlyout;

    private const double FocusInfoViewWidthRatio = 0.35;
    private const double FocusInfoViewHeightRatio = 0.9;
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public EditorCanvasView()
    {
        InitializeComponent();

        _menuFlyout = (MenuFlyout?)MainGrid.ContextFlyout ?? throw new ArgumentNullException();
        PointerWheelChanged += OnPointerWheelChanged;
        PointerMoved += OnPointerMoved;
        PointerExited += OnPointerExited;
        PointerPressed += OnPointerPressed;
        PointerReleased += OnPointerReleased;
        Initialized += OnInitialized;
        _viewModel = App.Current.Services.GetRequiredService<EditorCanvasViewModel>();
        _screenshotService = App.Current.Services.GetRequiredService<ScreenshotService>();
        DataContext = _viewModel;

        WeakReferenceMessenger.Default.Register<SaveFocusTreeToPngMessage>(this, SaveToPng);
    }

    private void OnInitialized(object? sender, EventArgs e)
    {
        // 方便右键菜单在前端绑定 Command
        foreach (MenuItem? item in _menuFlyout.Items)
        {
            item?.DataContext = this;
        }
    }

    private async void SaveToPng(object o, SaveFocusTreeToPngMessage saveFocusTreeToPngMessage)
    {
        var nodes = _viewModel.Nodes;
        if (nodes.Count == 0)
        {
            await ShowErrorMessageBoxAsync("没有可显示的国策", "错误");
            return;
        }

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null)
        {
            return;
        }

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(
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
            await ShowErrorMessageBoxAsync("导出图片失败", "错误");
        }
    }

    private static async Task ShowErrorMessageBoxAsync(string message, string title)
    {
        var box = MessageBoxManager.GetMessageBoxStandard(title, message, icon: Icon.Error);
        if (
            App.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime
            {
                MainWindow: not null
            } desktop
        )
        {
            await box.ShowWindowDialogAsync(desktop.MainWindow);
        }
        else
        {
            await box.ShowAsync();
        }
    }

    private static async Task<ButtonResult> ShowConfirmMessageBoxAsync(string message, string title)
    {
        var box = MessageBoxManager.GetMessageBoxStandard(title, message, ButtonEnum.YesNo, Icon.Warning);
        if (
            App.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime
            {
                MainWindow: not null
            } desktop
        )
        {
            return await box.ShowWindowDialogAsync(desktop.MainWindow);
        }
        return await box.ShowAsync();
    }

    private void OnPointerRightButtonDown(PointerPressedEventArgs e)
    {
        _lastRightClickPoint = e.GetPosition(this);
        var hitVisual = GetHitFocusNodeViewModel(_lastRightClickPoint);
        _lastRightClickFocus = hitVisual?.Model;

        // 连接模式时右键取消连接模式
        if (FocusConnectionMode != ConnectionType.None)
        {
            FocusConnectionMode = ConnectionType.None;
        }
        else
        {
            _menuFlyout.ShowAt(this, true);
        }
    }

    private FocusNodeViewModel? GetHitFocusNodeViewModel(Point position)
    {
        var visuals = this.GetVisualsAt(position);
        foreach (var visual in visuals)
        {
            if (visual is Control { DataContext: FocusNodeViewModel viewModel })
            {
                return viewModel;
            }
        }
        return null;
    }

    [RelayCommand(CanExecute = nameof(CursorOverFocus))]
    private void SetMutuallyExclusiveFocus()
    {
        if (_lastRightClickFocus is null)
        {
            return;
        }

        FocusConnectionMode = ConnectionType.MutuallyExclusive;
        ConnectionPreviewOverlay.From = _lastRightClickFocus;
    }

    [RelayCommand(CanExecute = nameof(CursorOverFocus))]
    private void SetPrerequisiteFocus()
    {
        if (_lastRightClickFocus is null)
        {
            return;
        }

        FocusConnectionMode = ConnectionType.Prerequisite;
        ConnectionPreviewOverlay.From = _lastRightClickFocus;
    }

    [RelayCommand(CanExecute = nameof(CursorOverFocus))]
    private async Task DeleteFocusNode()
    {
        var focus = _lastRightClickFocus;
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
            var result = await ShowConfirmMessageBoxAsync(
                $"有其他国策使用这个国策的相对位置, 删除后会导致这些国策的位置变更为绝对位置, 是否确认删除?\n\n受影响节点:\n\n{impactedFocusIds}",
                "确认删除"
            );
            if (result != ButtonResult.Yes)
            {
                return;
            }
        }

        //TODO: 删除后App内弹出提示

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
        _lastRightClickFocus = null;

        Debug.Assert(ConnectionPreviewOverlay.From != focus && ConnectionPreviewOverlay.To != focus);
    }

    [RelayCommand(CanExecute = nameof(CursorOverFocus))]
    private void SetRelativePosition()
    {
        if (_lastRightClickFocus is null)
        {
            return;
        }

        FocusConnectionMode = ConnectionType.RelativePosition;
        ConnectionPreviewOverlay.From = _lastRightClickFocus;
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        var props = e.GetCurrentPoint(this).Properties;
        if (props.PointerUpdateKind == PointerUpdateKind.LeftButtonReleased)
        {
            _movedFocusNode = null;
        }
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var props = e.GetCurrentPoint(this).Properties;

        if (props.IsRightButtonPressed)
        {
            OnPointerRightButtonDown(e);
            return;
        }

        if (!props.IsLeftButtonPressed)
        {
            return;
        }

        var position = e.GetPosition(this);
        var hitViewModel = GetHitFocusNodeViewModel(position);
        if (hitViewModel is not null)
        {
            if (
                _lastRightClickFocus is not null
                && FocusConnectionMode != ConnectionType.None
                && _lastRightClickFocus != hitViewModel.Model
            )
            {
                SetFocusConnection(hitViewModel);
                return;
            }

            if (e.ClickCount <= 1)
            {
                _movedFocusNode = hitViewModel.Model;
                FocusInfoViewControl.IsOpen = false;
                return;
            }

            _movedFocusNode = null;
            if (
                FocusInfoViewControl.DataContext is FocusInfoViewModel oldViewModel
                && oldViewModel.FocusNode == hitViewModel.Model
            )
            {
                FocusInfoViewControl.IsOpen = true;
                Log.Debug("相同国策, 跳过切换");
                return;
            }

            OpenFocusInfoView(hitViewModel.Model);
            Log.Debug("信息卡切换到国策: {Name}", hitViewModel.Model.Id);
        }
        else
        {
            FocusInfoViewControl.IsOpen = false;
        }
    }

    private void SetFocusConnection(FocusNodeViewModel viewModel)
    {
        Debug.Assert(_lastRightClickFocus is not null);

        _viewModel.CreateConnection(_lastRightClickFocus, viewModel.Model, FocusConnectionMode);

        _lastRightClickFocus = null;
        FocusConnectionMode = ConnectionType.None;
        ConnectionPreviewOverlay.ClearPreview();
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        var props = e.GetCurrentPoint(this).Properties;

        if (FocusConnectionMode != ConnectionType.None)
        {
            Cursor = new Cursor(StandardCursorType.Cross);
            var position = e.GetPosition(this);
            var hitViewModel = GetHitFocusNodeViewModel(position);
            if (hitViewModel is not null)
            {
                // TODO: 预览显示连接线之前检查合法性
                ConnectionPreviewOverlay.To = hitViewModel.Model;
            }
            else
            {
                ConnectionPreviewOverlay.To = null;
            }
        }

        if (props.IsMiddleButtonPressed)
        {
            if (_lastMousePositionOnCanvas.HasValue)
            {
                var currentMousePosition = e.GetPosition(this);
                var delta = currentMousePosition - _lastMousePositionOnCanvas.Value;

                _viewModel.TranslateX += delta.X;
                _viewModel.TranslateY += delta.Y;

                Cursor = new Cursor(StandardCursorType.Hand);
            }

            _lastMousePositionOnCanvas = e.GetPosition(this);
        }
        else
        {
            _lastMousePositionOnCanvas = null;
            // 在设置国策间条件时, 鼠标样式会被设置为 Cross, 为了防止被覆盖, 需要在这里检查
            if (FocusConnectionMode == ConnectionType.None)
            {
                Cursor = Cursor.Default;
            }
        }

        // 设置连接线时禁止拖动国策
        if (
            FocusConnectionMode == ConnectionType.None
            && props.IsLeftButtonPressed
            && _movedFocusNode is not null
        )
        {
            var position = GetMousePositionOnGrid(e.GetPosition(this));
            _movedFocusNode.SetRawPosition(position.X, position.Y);
        }
    }

    private void OnPointerExited(object? sender, PointerEventArgs e)
    {
        _lastMousePositionOnCanvas = null;
        Cursor = Cursor.Default;
    }

    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        // 缩放
        const double scaleRate = 1.1;
        var mousePoint = e.GetPosition(this);

        double newScale;

        if (e.Delta.Y > 0)
        {
            // 放大
            newScale = _viewModel.Scale * scaleRate;
        }
        else
        {
            // 缩小
            newScale = _viewModel.Scale / scaleRate;
        }

        if (newScale < 0.1)
        {
            newScale = 0.1;
        }
        if (newScale > 5.0)
        {
            newScale = 5.0;
        }

        double oldScale = _viewModel.Scale;
        _viewModel.TranslateX = mousePoint.X - (mousePoint.X - _viewModel.TranslateX) * (newScale / oldScale);
        _viewModel.TranslateY = mousePoint.Y - (mousePoint.Y - _viewModel.TranslateY) * (newScale / oldScale);

        _viewModel.Scale = newScale;
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
            var position = GetMousePositionOnGrid(_lastRightClickPoint);
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

    private bool CanConvertToAbsolutePosition => _lastRightClickFocus?.RelativePosition is not null;

    [RelayCommand(CanExecute = nameof(CanConvertToAbsolutePosition))]
    private void ConvertToAbsolutePosition()
    {
        if (_lastRightClickFocus is null)
        {
            return;
        }

        _lastRightClickFocus.ConvertToAbsolutePosition();
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

    /// <summary>
    /// 获取鼠标在网格中的实际坐标
    /// </summary>
    /// <param name="mousePoint">原始坐标</param>
    /// <returns></returns>
    private (int X, int Y) GetMousePositionOnGrid(Point mousePoint)
    {
        double rX = mousePoint.X - _viewModel.TranslateX;
        double rY = mousePoint.Y - _viewModel.TranslateY;
        double width = FocusMapConstants.CellWidth * _viewModel.Scale;
        double height = FocusMapConstants.CellHeight * _viewModel.Scale;

        int x = (int)Math.Floor(rX / width);
        int y = (int)Math.Floor(rY / height);

        return (x, y);
    }

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
}
