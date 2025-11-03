using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Hoi4BlueprintEditor.ViewsModels;
using Microsoft.Extensions.DependencyInjection;
using NLog;

namespace Hoi4BlueprintEditor.Views;

public sealed partial class EditorCanvasView : UserControl
{
    private Point? _lastMousePositionOnCanvas;
    private readonly EditorCanvasViewModel _viewModel;

    private const double FocusInfoViewWidthRatio = 0.35;
    private const double FocusInfoViewHeightRatio = 0.9;
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
    }

    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var position = e.GetPosition(this);
        var result = VisualTreeHelper.HitTest(this, position);
        if (result.VisualHit is FrameworkElement { DataContext: FocusNodeViewModel viewModel })
        {
            if (
                FocusInfoView.DataContext is FocusInfoViewModel oldViewModel
                && oldViewModel.FocusNode == viewModel.Model
            )
            {
                Log.Debug("相同国策, 跳过切换");
                return;
            }

            FocusInfoView.Width = ActualWidth * FocusInfoViewWidthRatio;
            FocusInfoView.Height = ActualHeight * FocusInfoViewHeightRatio;

            FocusInfoView.DataContext = new FocusInfoViewModel(viewModel.Model);
            FocusInfoView.IsOpen = true;
            Log.Debug("切换国策: {Name}", viewModel.Model.Id);
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

    // 点击信息面板时阻止事件冒泡, 导致点击FocusInfoView时关闭面板
    private void FocusInfoView_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
    }
}
