using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Hoi4BlueprintEditor.Constants;
using Hoi4BlueprintEditor.Messages;
using Hoi4BlueprintEditor.Models;
using Hoi4BlueprintEditor.Models.Focus;
using Hoi4BlueprintEditor.Services;
using Hoi4BlueprintEditor.Services.GameResources.Localization;
using Hoi4BlueprintEditor.Views.Dialogs;
using Hoi4BlueprintEditor.ViewsModels;
using Hoi4BlueprintEditor.ViewsModels.Dialogs;
using iNKORE.UI.WPF.Modern.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using NLog;
using ZLinq;
using MessageBox = iNKORE.UI.WPF.Modern.Controls.MessageBox;

namespace Hoi4BlueprintEditor.Views;

public sealed partial class EditorCanvasView : UserControl
{
    private Point? _lastMousePositionOnCanvas;
    private FocusNode? _movedFocusNode;
    private readonly EditorCanvasViewModel _viewModel;
    private FocusNode? _lastRightClickFocus;
    private Point _lastRightClickPoint;
    private readonly ScreenshotService _screenshotService;

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

    private const double FocusInfoViewWidthRatio = 0.35;
    private const double FocusInfoViewHeightRatio = 0.9;
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private static readonly LocalizationFormatService LocalizationFormatService =
        App.Current.Services.GetRequiredService<LocalizationFormatService>();

    public EditorCanvasView()
    {
        InitializeComponent();

        MouseWheel += OnMouseWheel;
        MouseMove += OnMouseMove;
        MouseLeave += OnMouseLeave;
        MouseLeftButtonDown += OnMouseLeftButtonDown;
        MouseLeftButtonUp += OnMouseLeftButtonUp;
        MouseRightButtonDown += OnMouseRightButtonDown;
        _viewModel = App.Current.Services.GetRequiredService<EditorCanvasViewModel>();
        _screenshotService = App.Current.Services.GetRequiredService<ScreenshotService>();
        DataContext = _viewModel;

        // 方便右键菜单在前端绑定 Command
        RightContextMenu.DataContext = this;
        WeakReferenceMessenger.Default.Register<SaveFocusTreeToPngMessage>(this, SaveToPng);
    }

    private void SaveToPng(object o, SaveFocusTreeToPngMessage saveFocusTreeToPngMessage)
    {
        var nodes = _viewModel.Nodes;
        if (nodes.Count == 0)
        {
            MessageBox.Show("没有可显示的国策", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        var dialog = new SaveFileDialog
        {
            Title = "导出国策树为图片",
            DefaultExt = ".png",
            Filter = "PNG 图片 (*.png)|*.png",
            FileName = "FocusTree.png"
        };

        if (dialog.ShowDialog() != true || string.IsNullOrWhiteSpace(dialog.FileName))
        {
            return;
        }

        var notificationService = App.Current.Services.GetRequiredService<NotificationService>();
        try
        {
            _screenshotService.SaveFocusTreeScreenshot(nodes, dialog.FileName);
            Log.Info("已导出图片: {FileName}", dialog.FileName);
            notificationService.Show("导出图片成功");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "导出国策树图片失败");
            MessageBox.Show("导出图片失败", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        _lastRightClickPoint = e.GetPosition(this);
        var result = VisualTreeHelper.HitTest(this, _lastRightClickPoint);
        if (result.VisualHit is FrameworkElement { DataContext: FocusNodeViewModel viewModel })
        {
            _lastRightClickFocus = viewModel.Model;
        }
        else
        {
            _lastRightClickFocus = null;
        }

        // 连接模式时右键取消连接模式
        if (FocusConnectionMode != ConnectionType.None)
        {
            FocusConnectionMode = ConnectionType.None;
        }
        else
        {
            RightContextMenu.IsOpen = true;
        }
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
    private void DeleteFocusNode()
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
                .Select(static f => LocalizationFormatService.GetFormatText(f.Id))
                .JoinToString('\n');
            var result = MessageBox.Show(
                $"有其他国策使用这个国策的相对位置, 删除后会导致这些国策的位置变更为绝对位置, 是否确认删除?\n\n受影响节点:\n\n{impactedFocusIds}",
                "确认删除",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning
            );
            if (result != MessageBoxResult.Yes)
            {
                return;
            }
        }

        //TODO: 删除后App内弹出提示

        // 关闭信息卡, 并释放 ViewModel 资源, 防止内存泄漏
        if (FocusInfoView.DataContext is FocusInfoViewModel infoViewModel && infoViewModel.FocusNode == focus)
        {
            FocusInfoView.IsOpen = false;
            FocusInfoView.DataContext = null;
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

    private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _movedFocusNode = null;
    }

    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var position = e.GetPosition(this);
        var result = VisualTreeHelper.HitTest(this, position);
        if (result.VisualHit is FrameworkElement { DataContext: FocusNodeViewModel viewModel })
        {
            if (
                _lastRightClickFocus is not null
                && FocusConnectionMode != ConnectionType.None
                && _lastRightClickFocus != viewModel.Model
            )
            {
                SetFocusConnection(viewModel);
                return;
            }

            if (e.ClickCount <= 1)
            {
                _movedFocusNode = viewModel.Model;
                FocusInfoView.IsOpen = false;
                return;
            }

            _movedFocusNode = null;
            if (
                FocusInfoView.DataContext is FocusInfoViewModel oldViewModel
                && oldViewModel.FocusNode == viewModel.Model
            )
            {
                FocusInfoView.IsOpen = true;
                Log.Debug("相同国策, 跳过切换");
                return;
            }

            OpenFocusInfoView(viewModel.Model);
            Log.Debug("信息卡切换到国策: {Name}", viewModel.Model.Id);
        }
        else
        {
            FocusInfoView.IsOpen = false;
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

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (FocusConnectionMode != ConnectionType.None)
        {
            Cursor = Cursors.Cross;
            var position = e.GetPosition(this);
            var result = VisualTreeHelper.HitTest(this, position);
            if (result.VisualHit is FrameworkElement { DataContext: FocusNodeViewModel viewModel })
            {
                // TODO: 预览显示连接线之前检查合法性
                ConnectionPreviewOverlay.To = viewModel.Model;
            }
            else
            {
                ConnectionPreviewOverlay.To = null;
            }
        }

        if (e.MiddleButton == MouseButtonState.Pressed)
        {
            if (_lastMousePositionOnCanvas.HasValue)
            {
                var currentMousePosition = e.GetPosition(this);
                var delta = currentMousePosition - _lastMousePositionOnCanvas.Value;

                _viewModel.TranslateX += delta.X;
                _viewModel.TranslateY += delta.Y;

                Cursor = Cursors.Hand;
            }

            _lastMousePositionOnCanvas = e.GetPosition(this);
        }
        else
        {
            _lastMousePositionOnCanvas = null;
            // 在设置国策间条件时, 鼠标样式会被设置为 Cross, 为了防止被覆盖, 需要在这里检查
            if (FocusConnectionMode == ConnectionType.None)
            {
                Cursor = Cursors.Arrow;
            }
        }

        // 设置连接线时禁止拖动国策
        if (
            FocusConnectionMode == ConnectionType.None
            && e.LeftButton == MouseButtonState.Pressed
            && _movedFocusNode is not null
        )
        {
            var position = GetMousePositionOnGrid(e.GetPosition(this));
            _movedFocusNode.SetRawPosition(position.X, position.Y);
        }
    }

    private void OnMouseLeave(object sender, MouseEventArgs e)
    {
        _lastMousePositionOnCanvas = null;
        Cursor = Cursors.Arrow;
    }

    private void OnMouseWheel(object sender, MouseWheelEventArgs e)
    {
        // 缩放
        const double scaleRate = 1.1;
        var mousePoint = e.GetPosition(this);

        double newScale;

        if (e.Delta > 0)
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
        var result = await dialog.ShowAsync(App.Current.MainWindow);

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

    private void ContextMenu_OnOpened(object sender, RoutedEventArgs e)
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
        FocusInfoView.DataContext = new FocusInfoViewModel(focusNode);
        FocusInfoView.Width = ActualWidth * FocusInfoViewWidthRatio;
        FocusInfoView.Height = ActualHeight * FocusInfoViewHeightRatio;
        FocusInfoView.IsOpen = true;
    }
}
