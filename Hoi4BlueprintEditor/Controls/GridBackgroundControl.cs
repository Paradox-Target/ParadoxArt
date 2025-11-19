using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Hoi4BlueprintEditor.Extensions;
using Hoi4BlueprintEditor.Helpers;

namespace Hoi4BlueprintEditor.Controls;

public sealed class GridBackgroundControl : Control
{
    // 标尺大小
    private const double RulerSize = 30.0;
    private static readonly SolidColorBrush RulerBrush =
        "#4A4A4A".ToBrush() ?? new SolidColorBrush(Colors.Brown);
    private static readonly Pen GridPen = new("#444444".ToBrush(), 1.0);

    #region Dependency Proerties (依赖属性)

    // 接收ViewModel的Scale和Translate

    public static readonly DependencyProperty ScaleProperty = DependencyProperty.Register(
        nameof(Scale),
        typeof(double),
        typeof(GridBackgroundControl),
        new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.AffectsRender)
    );

    public double Scale
    {
        get => (double)GetValue(ScaleProperty);
        set => SetValue(ScaleProperty, value);
    }

    public static readonly DependencyProperty TranslateXProperty = DependencyProperty.Register(
        nameof(TranslateX),
        typeof(double),
        typeof(GridBackgroundControl),
        new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender)
    );

    public double TranslateX
    {
        get => (double)GetValue(TranslateXProperty);
        set => SetValue(TranslateXProperty, value);
    }

    public static readonly DependencyProperty TranslateYProperty = DependencyProperty.Register(
        nameof(TranslateY),
        typeof(double),
        typeof(GridBackgroundControl),
        new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender)
    );

    public double TranslateY
    {
        get => (double)GetValue(TranslateYProperty);
        set => SetValue(TranslateYProperty, value);
    }

    #endregion

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        
        // 绘制垂直网格线和X轴标尺
        GridDrawHelper.GetXRange(TranslateX, Scale, ActualWidth, out int startX, out int endX);
        for (int i = startX; i <= endX; i++)
        {
            double xPos = GridDrawHelper.GetX(TranslateX, Scale, i);
            dc.DrawLine(GridPen, new Point(xPos, 0), new Point(xPos, ActualHeight));
        }

        // 绘制水平网格线和Y轴标尺
        GridDrawHelper.GetXRange(TranslateY, Scale, ActualHeight, out int startY, out int endY);
        for (int i = startY; i <= endY; i++)
        {
            double yPos = GridDrawHelper.GetY(TranslateY, Scale, i);
            dc.DrawLine(GridPen, new Point(0, yPos), new Point(ActualWidth, yPos));
        }
    }
}
