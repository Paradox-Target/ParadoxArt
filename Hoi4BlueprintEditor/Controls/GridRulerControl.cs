using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Hoi4BlueprintEditor.Extensions;

namespace Hoi4BlueprintEditor.Controls;

public sealed class GridRulerControl : Control
{
    public static readonly double CellWidth;
    public static readonly double CellHeight;

    static GridRulerControl()
    {
        CellWidth = (double)App.Current.FindResource("FocusNodeWidth");
        CellHeight = (double)App.Current.FindResource("FocusNodeHeight");
    }

    // 标尺大小
    private const double RulerSize = 30.0;
    private static readonly SolidColorBrush RulerBrush =
        "#4A4A4A".ToBrush() ?? new SolidColorBrush(Colors.Brown);
    private static readonly Pen GridPen = new("#444444".ToBrush(), 1.0);
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

        #region 绘制垂直网格线和X轴标尺

        int startX = (int)Math.Max(0, -TranslateX / (CellWidth * Scale));
        // 200随便设置的上限
        int endX = (int)Math.Min(200, (ActualWidth - TranslateX) / (CellWidth * Scale));

        for (int i = startX; i <= endX; i++)
        {
            // X坐标
            double xPos = TranslateX + i * CellWidth * Scale;

            // 垂直网格线
            dc.DrawLine(GridPen, new Point(xPos, 0), new Point(xPos, ActualHeight));

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

        #region 绘制水平网格线和Y轴标尺

        int startY = (int)Math.Max(0, -TranslateY / (CellHeight * Scale));
        int endY = (int)Math.Min(200, (ActualHeight - TranslateY) / (CellHeight * Scale));

        for (int i = startY; i <= endY; i++)
        {
            // Y坐标
            double yPos = TranslateY + i * CellHeight * Scale;

            // 水平网格线
            dc.DrawLine(GridPen, new Point(0, yPos), new Point(ActualWidth, yPos));

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
