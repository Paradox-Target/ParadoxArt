using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Hoi4BlueprintBuilder.Core.Extensions;
using Hoi4BlueprintBuilder.Core.Helpers;

namespace Hoi4BlueprintBuilder.Core.Controls;

public sealed class GridRulerControl : Control
{
    // 标尺大小
    private const double RulerSize = 30.0;
    private static readonly SolidColorBrush RulerBrush = new(Color.FromRgb(74, 74, 74));
    private static readonly Typeface RulerTypeface = new("Segoe UI");

    #region Styled Properties (样式属性)

    // 接收ViewModel的Scale和Translate

    public static readonly StyledProperty<double> ScaleProperty = AvaloniaProperty.Register<
        GridRulerControl,
        double
    >(nameof(Scale), 1.0);

    public double Scale
    {
        get => GetValue(ScaleProperty);
        set => SetValue(ScaleProperty, value);
    }

    public static readonly StyledProperty<double> TranslateXProperty = AvaloniaProperty.Register<
        GridRulerControl,
        double
    >(nameof(TranslateX), 0.0);

    public double TranslateX
    {
        get => GetValue(TranslateXProperty);
        set => SetValue(TranslateXProperty, value);
    }

    public static readonly StyledProperty<double> TranslateYProperty = AvaloniaProperty.Register<
        GridRulerControl,
        double
    >(nameof(TranslateY), 0.0);

    public double TranslateY
    {
        get => GetValue(TranslateYProperty);
        set => SetValue(TranslateYProperty, value);
    }

    static GridRulerControl()
    {
        AffectsRender<GridRulerControl>(ScaleProperty, TranslateXProperty, TranslateYProperty);
    }

    #endregion

    public override void Render(DrawingContext dc)
    {
        base.Render(dc);

        // 绘制标尺背景
        dc.DrawRectangle(RulerBrush, null, new Rect(0, 0, RulerSize, Bounds.Height));
        dc.DrawRectangle(RulerBrush, null, new Rect(0, Bounds.Height - RulerSize, Bounds.Width, RulerSize));

        bool canDrawText = Scale > 0.3;

        #region 绘制垂直标尺

        (int startX, int endX) = GridDrawHelper.GetXRange(TranslateX, Scale, Bounds.Width);
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
                    Brushes.WhiteSmoke
                );
                // 5内边距
                dc.DrawText(text, new Point(xPos + 5, Bounds.Height - RulerSize + 5));
            }
        }

        #endregion

        #region 绘制水平标尺

        (int startY, int endY) = GridDrawHelper.GetXRange(TranslateY, Scale, Bounds.Height);
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
                    Brushes.WhiteSmoke
                );
                // 5内边距
                dc.DrawText(text, new Point(5, yPos + 5));
            }
        }

        #endregion
    }
}
