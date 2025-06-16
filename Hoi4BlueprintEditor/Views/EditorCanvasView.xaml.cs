using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Hoi4BlueprintEditor.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Hoi4BlueprintEditor.Views;

public sealed partial class EditorCanvasView : UserControl
{
    private Point? _lastMousePositionOnCanvas;
    private readonly EditorCanvasViewModel _viewModel;

    public EditorCanvasView()
    {
        InitializeComponent();

        // 鼠标事件
        MouseWheel += OnMouseWheel;
        MouseMove += OnMouseMove;
        MouseLeave += OnMouseLeave;
        _viewModel = App.Current.Services.GetRequiredService<EditorCanvasViewModel>();
        DataContext = _viewModel;
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
}
