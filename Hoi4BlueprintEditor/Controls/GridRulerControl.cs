using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Hoi4BlueprintEditor.Extensions;
using Hoi4BlueprintEditor.Helpers;

namespace Hoi4BlueprintEditor.Controls;

public sealed class GridRulerControl : Control
{
    // 标尺大小
    private const double RulerSize = 30.0;
    private static readonly SolidColorBrush RulerBrush =
        "#4A4A4A".ToBrush() ?? new SolidColorBrush(Colors.Brown);
    private static readonly Typeface RulerTypeface = new("Segoe UI");

    #region Dependency Proerties (依赖属性)

    // 接收ViewModel的Scale和Translate

    public static readonly DependencyProperty ScaleProperty = DependencyProperty.Register(
        nameof(Scale),
        typeof(double),
        typeof(GridRulerControl),
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
        typeof(GridRulerControl),
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
        typeof(GridRulerControl),
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

        // 绘制标尺背景
        dc.DrawRectangle(RulerBrush, null, new Rect(0, 0, RulerSize, ActualHeight));
        dc.DrawRectangle(RulerBrush, null, new Rect(0, ActualHeight - RulerSize, ActualWidth, RulerSize));

        bool canDrawText = Scale > 0.3;

        #region 绘制垂直标尺

        (int startX, int endX) = GridDrawHelper.GetXRange(TranslateX, Scale, ActualWidth);
        for (int i = startX; i <= endX; i++)
        {
            // X坐标
            double xPos = GridDrawHelper.GetX(TranslateX, Scale, i);
            if (canDrawText)
            {
                // 画底部标尺数字
                var text = new FormattedText(
                    i.ToString(),
                    CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    RulerTypeface,
                    12,
                    Brushes.WhiteSmoke,
                    1.25
                );
                // 5内边距
                dc.DrawText(text, new Point(xPos + 5, ActualHeight - RulerSize + 5));
            }
        }

        #endregion

        #region 绘制水平标尺

        (int startY, int endY) = GridDrawHelper.GetXRange(TranslateY, Scale, ActualHeight);
        for (int i = startY; i <= endY; i++)
        {
            // Y坐标
            double yPos = GridDrawHelper.GetY(TranslateY, Scale, i);
            if (canDrawText)
            {
                // 画左侧标尺的数字
                var text = new FormattedText(
                    i.ToString(),
                    CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    RulerTypeface,
                    12,
                    Brushes.WhiteSmoke,
                    1.25
                );
                // 5内边距
                dc.DrawText(text, new Point(5, yPos + 5));
            }
        }

        #endregion
    }
}
