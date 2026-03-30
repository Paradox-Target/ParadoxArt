using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Hoi4BlueprintBuilder.Core.Extensions;
using Hoi4BlueprintBuilder.Core.Helpers;
using Hoi4BlueprintBuilder.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Hoi4BlueprintBuilder.Core.Controls;

public sealed class GridBackgroundControl : Control
{
    private static readonly IPen GridPen = InitializeGridPen();
    private static readonly ProjectConfigService ProjectConfigService =
        App.Current.Services.GetRequiredService<ProjectConfigService>();

    private static Pen InitializeGridPen()
    {
        var brush = "#444444".ToBrush();
        return new Pen(brush ?? Brushes.Gray);
    }

    static GridBackgroundControl()
    {
        AffectsRender<GridBackgroundControl>(ScaleProperty, TranslateXProperty, TranslateYProperty);
    }

    #region Styled Properties

    public static readonly StyledProperty<double> ScaleProperty = AvaloniaProperty.Register<
        GridBackgroundControl,
        double
    >(nameof(Scale), defaultValue: 1.0);

    public double Scale
    {
        get => GetValue(ScaleProperty);
        set => SetValue(ScaleProperty, value);
    }

    public static readonly StyledProperty<double> TranslateXProperty = AvaloniaProperty.Register<
        GridBackgroundControl,
        double
    >(nameof(TranslateX), defaultValue: 0.0);

    public double TranslateX
    {
        get => GetValue(TranslateXProperty);
        set => SetValue(TranslateXProperty, value);
    }

    public static readonly StyledProperty<double> TranslateYProperty = AvaloniaProperty.Register<
        GridBackgroundControl,
        double
    >(nameof(TranslateY), defaultValue: 0.0);

    public double TranslateY
    {
        get => GetValue(TranslateYProperty);
        set => SetValue(TranslateYProperty, value);
    }

    #endregion

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        // 绘制垂直网格线和X轴标尺
        (int startX, int endX) = GridDrawHelper.GetXRange(
            TranslateX,
            Scale,
            Bounds.Width,
            ProjectConfigService.FocusCellWidth
        );
        for (int i = startX; i <= endX; i++)
        {
            double xPos = GridDrawHelper.GetX(TranslateX, Scale, i, ProjectConfigService.FocusCellWidth);
            context.DrawLine(GridPen, new Point(xPos, 0), new Point(xPos, Bounds.Height));
        }

        // 绘制水平网格线和Y轴标尺
        (int startY, int endY) = GridDrawHelper.GetXRange(
            TranslateY,
            Scale,
            Bounds.Height,
            ProjectConfigService.FocusCellWidth
        );
        for (int i = startY; i <= endY; i++)
        {
            double yPos = GridDrawHelper.GetY(TranslateY, Scale, i, ProjectConfigService.FocusCellHeight);
            context.DrawLine(GridPen, new Point(0, yPos), new Point(Bounds.Width, yPos));
        }
    }
}
