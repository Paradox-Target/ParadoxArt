using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Hoi4BlueprintBuilder.Core.Constants;

namespace Hoi4BlueprintBuilder.Core.Controls;

/// <summary>
/// 框选矩形控件，用于显示框选区域
/// </summary>
public sealed class SelectionRectangleControl : Control
{
    private static readonly IBrush FillBrush = new SolidColorBrush(Color.FromArgb(40, 0, 120, 215));
    private static readonly IPen StrokePen = new Pen(new SolidColorBrush(Color.FromRgb(0, 120, 215)), 1);

    static SelectionRectangleControl()
    {
        AffectsRender<SelectionRectangleControl>(IsVisibleProperty, StartPointProperty, EndPointProperty);
    }

    #region Styled Properties

    public static readonly StyledProperty<Point> StartPointProperty = AvaloniaProperty.Register<
        SelectionRectangleControl,
        Point
    >(nameof(StartPoint));

    /// <summary>
    /// 框选起点（屏幕坐标）
    /// </summary>
    public Point StartPoint
    {
        get => GetValue(StartPointProperty);
        set => SetValue(StartPointProperty, value);
    }

    public static readonly StyledProperty<Point> EndPointProperty = AvaloniaProperty.Register<
        SelectionRectangleControl,
        Point
    >(nameof(EndPoint));

    /// <summary>
    /// 框选终点（屏幕坐标）
    /// </summary>
    public Point EndPoint
    {
        get => GetValue(EndPointProperty);
        set => SetValue(EndPointProperty, value);
    }

    #endregion

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        if (!IsVisible)
        {
            return;
        }

        var rect = GetSelectionRect();
        context.DrawRectangle(FillBrush, StrokePen, rect);
    }

    /// <summary>
    /// 获取选择矩形（确保宽高为正值）
    /// </summary>
    public Rect GetSelectionRect()
    {
        double x = Math.Min(StartPoint.X, EndPoint.X);
        double y = Math.Min(StartPoint.Y, EndPoint.Y);
        double width = Math.Abs(EndPoint.X - StartPoint.X);
        double height = Math.Abs(EndPoint.Y - StartPoint.Y);
        return new Rect(x, y, width, height);
    }

    /// <summary>
    /// 重置框选区域
    /// </summary>
    public void Reset()
    {
        StartPoint = default;
        EndPoint = default;
        IsVisible = false;
    }
}
