using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Hoi4BlueprintEditor.ViewModels;

namespace Hoi4BlueprintEditor.Views;

public sealed partial class EditorCanvasView : UserControl
{
    private Point? _lastMousePositionOnCanvas;

    public EditorCanvasView()
    {
        InitializeComponent();

        // 鼠标事件
        MouseWheel += OnMouseWheel;
        MouseMove += OnMouseMove;
        MouseLeave += OnMouseLeave;
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        var canvasVm = DataContext as EditorCanvasViewModel;
        if (canvasVm == null)
        {
            return;
        }

        if (e.RightButton == MouseButtonState.Pressed)
        {
            if (_lastMousePositionOnCanvas.HasValue)
            {
                var currentMousePosition = e.GetPosition(this);
                var delta = currentMousePosition - _lastMousePositionOnCanvas.Value;

                canvasVm.TranslateX += delta.X;
                canvasVm.TranslateY += delta.Y;

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

    private void OnMouseWheel(object sender, MouseWheelEventArgs e) // 缩放
    {
        var canvasVm = (EditorCanvasViewModel)DataContext;
        if (canvasVm == null)
        {
            return;
        }

        const double scaleRate = 1.1;
        var mousePoint = e.GetPosition(this);

        double newScale;
        if (e.Delta > 0) // 放大
        {
            newScale = canvasVm.Scale * scaleRate;
        }
        else // 缩小
        {
            newScale = canvasVm.Scale / scaleRate;
        }

        if (newScale < 0.1)
        {
            newScale = 0.1;
        }
        if (newScale > 5.0)
        {
            newScale = 5.0;
        }

        var oldScale = canvasVm.Scale;
        canvasVm.TranslateX = mousePoint.X - (mousePoint.X - canvasVm.TranslateX) * (newScale / oldScale);
        canvasVm.TranslateY = mousePoint.Y - (mousePoint.Y - canvasVm.TranslateY) * (newScale / oldScale);

        canvasVm.Scale = newScale;
    }
}
