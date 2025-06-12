using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Hoi4BlueprintEditor.ViewModels;

namespace Hoi4BlueprintEditor.Views;

public partial class EditorCanvasView : UserControl
{
    private bool _isPanning;
    private Point _panStartPoint;

    public EditorCanvasView()
    {
        InitializeComponent();

        // 鼠标事件
        MouseWheel += OnMouseWheel;
        PreviewMouseDown += OnPreviewMouseDown;
        PreviewMouseUp += OnPreviewMouseUp;
        MouseMove += OnMouseMove;
        MouseLeave += (_, _) =>
        {
            _isPanning = false;
        };
    }

    private void OnPreviewMouseDown(object sender, MouseButtonEventArgs e) //中键平移
    {
        // 画布事件
        if (e.ChangedButton == MouseButton.Middle && e.ButtonState == MouseButtonState.Pressed)
        {
            var sourceElement = e.Source as DependencyObject;
            bool isNode = false;
            while (sourceElement != null)
            {
                if (sourceElement is FocusNodeView)
                {
                    isNode = true;
                    break;
                }
                sourceElement = System.Windows.Media.VisualTreeHelper.GetParent(sourceElement);
            }
            if (isNode) return;

            e.Handled = true;
            _isPanning = true;
            _panStartPoint = e.GetPosition(this); // 起始点
            Cursor = Cursors.Hand; // 鼠标图案改变
        }
    }

    private void OnPreviewMouseUp(object sender, MouseButtonEventArgs e) // 结束平移
    {
        if (e.ChangedButton == MouseButton.Middle && e.ButtonState == MouseButtonState.Released)
        {
            _isPanning = false;
            Cursor = Cursors.Arrow; // 恢复鼠标图案
        }
    }

    private void OnMouseMove(object sender, MouseEventArgs e) // 移动平移
    {
        if (_isPanning) return;

        var canvasVm = (EditorCanvasViewModel)DataContext;
        var currentPoint = e.GetPosition(this);

        var delta = currentPoint - _panStartPoint; // 计算向量

        canvasVm.TranslateX += delta.X;
        canvasVm.TranslateY += delta.Y;

        _panStartPoint = currentPoint; // 更新起始点
    }

    private void OnMouseWheel(object sender, MouseWheelEventArgs e) // 缩放
    {
        var canvasVm = (EditorCanvasViewModel)DataContext;
        if (canvasVm == null) return;

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

        if (newScale < 0.1) newScale = 0.1;
        if (newScale > 5.0) newScale = 5.0;

        var oldScale = canvasVm.Scale;
        canvasVm.TranslateX = mousePoint.X - (mousePoint.X - canvasVm.TranslateX) * (newScale / oldScale);
        canvasVm.TranslateY = mousePoint.Y - (mousePoint.Y - canvasVm.TranslateY) * (newScale / oldScale);

        canvasVm.Scale = newScale;
    }
}
