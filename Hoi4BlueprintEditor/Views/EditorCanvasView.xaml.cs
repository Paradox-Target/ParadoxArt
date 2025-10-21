using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using CommunityToolkit.Mvvm.Messaging;
using Hoi4BlueprintEditor.Messages;
using Hoi4BlueprintEditor.ViewsModels;
using Microsoft.Extensions.DependencyInjection;
using NLog;

namespace Hoi4BlueprintEditor.Views;

public sealed partial class EditorCanvasView : UserControl
{
    private Point? _lastMousePositionOnCanvas;
    private readonly EditorCanvasViewModel _viewModel;
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public EditorCanvasView()
    {
        InitializeComponent();

        // 鼠标事件
        MouseWheel += OnMouseWheel;
        MouseMove += OnMouseMove;
        MouseLeave += OnMouseLeave;
        MouseLeftButtonDown += OnMouseLeftButtonDown;
        _viewModel = App.Current.Services.GetRequiredService<EditorCanvasViewModel>();
        DataContext = _viewModel;
        FocusInfoView.CustomPopupPlacementCallback = CustomPopupPlacement;

        // 主窗口改变时关闭信息面板
        WeakReferenceMessenger.Default.Register<MainWindowStateChangeMessage>(this, MainWindowStateChanged);
        WeakReferenceMessenger.Default.Register<MainWindowDeactivatedMessage>(
            this,
            (_, _) => FocusInfoView.IsOpen = false
        );
    }

    // TODO: 改变窗口大小或者移动窗口时关闭信息面板? (避免位置错乱)
    private void MainWindowStateChanged(object _, MainWindowStateChangeMessage message)
    {
        if (message.Sender is not Window window)
        {
            return;
        }

        if (window.WindowState == WindowState.Minimized)
        {
            FocusInfoView.IsOpen = false;
        }
        else if (window.WindowState is WindowState.Normal or WindowState.Maximized)
        {
            FocusInfoView.IsOpen = true;
        }
    }

    private static CustomPopupPlacement[] CustomPopupPlacement(Size popupSize, Size targetSize, Point offset)
    {
        var customPopupPlacement = new CustomPopupPlacement(
            new Point(targetSize.Width - popupSize.Width, (targetSize.Height - popupSize.Height) / 2),
            PopupPrimaryAxis.Vertical
        );
        return [customPopupPlacement];
    }

    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var position = e.GetPosition(this);
        var result = VisualTreeHelper.HitTest(this, position);
        if (
            result.VisualHit is FrameworkElement element
            && element.DataContext is FocusNodeViewModel viewModel
        )
        {
            FocusInfoView.Width = ActualWidth * 0.35;
            FocusInfoView.Height = ActualHeight * 0.9;

            FocusInfoView.IsOpen = true;
        }
        else
        {
            FocusInfoView.IsOpen = false;
        }
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (e.RightButton == MouseButtonState.Pressed)
        {
            if (_lastMousePositionOnCanvas.HasValue)
            {
                var currentMousePosition = e.GetPosition(this);
                var delta = currentMousePosition - _lastMousePositionOnCanvas.Value;

                _viewModel.TranslateX += delta.X;
                _viewModel.TranslateY += delta.Y;

                // 鼠标图案改变
                Cursor = Cursors.Hand;
            }

            _lastMousePositionOnCanvas = e.GetPosition(this);
        }
        else
        {
            _lastMousePositionOnCanvas = null;
            Cursor = Cursors.Arrow;
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

    private void EditorCanvasView_OnUnloaded(object sender, RoutedEventArgs e)
    {
        WeakReferenceMessenger.Default.UnregisterAll(this);
    }

    // 点击信息面板时阻止事件冒泡, 导致点击FocusInfoView时关闭面板
    private void FocusInfoView_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
    }
}
