using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Hoi4BlueprintBuilder.Core.Models;
using Hoi4BlueprintBuilder.Core.Models.Focus;
using Hoi4BlueprintBuilder.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Hoi4BlueprintBuilder.Core.Controls;

public sealed class ConnectionPreviewOverlayControl : Control
{
    public FocusNode? From
    {
        get;
        set
        {
            field = value;
            InvalidateVisual();
        }
    }

    public FocusNode? To
    {
        get;
        set
        {
            field = value;
            InvalidateVisual();
        }
    }

    private static readonly ProjectConfigService ProjectConfigService =
        App.Current.Services.GetRequiredService<ProjectConfigService>();

    static ConnectionPreviewOverlayControl()
    {
        AffectsRender<ConnectionPreviewOverlayControl>(ScaleProperty, TranslateXProperty, TranslateYProperty);
    }

    #region Styled Properties

    public static readonly StyledProperty<double> ScaleProperty = AvaloniaProperty.Register<
        ConnectionPreviewOverlayControl,
        double
    >(nameof(Scale), defaultValue: 1.0);

    public double Scale
    {
        get => GetValue(ScaleProperty);
        set => SetValue(ScaleProperty, value);
    }

    public static readonly StyledProperty<double> TranslateXProperty = AvaloniaProperty.Register<
        ConnectionPreviewOverlayControl,
        double
    >(nameof(TranslateX), defaultValue: 0.0);

    public double TranslateX
    {
        get => GetValue(TranslateXProperty);
        set => SetValue(TranslateXProperty, value);
    }

    public static readonly StyledProperty<double> TranslateYProperty = AvaloniaProperty.Register<
        ConnectionPreviewOverlayControl,
        double
    >(nameof(TranslateY), defaultValue: 0.0);

    public double TranslateY
    {
        get => GetValue(TranslateYProperty);
        set => SetValue(TranslateYProperty, value);
    }

    #endregion

    public ConnectionType Mode { get; set; }

    public override void Render(DrawingContext dc)
    {
        base.Render(dc);
        if (From is null || To is null || Mode == ConnectionType.None)
        {
            return;
        }

        var matrix = Matrix.CreateScale(Scale, Scale) * Matrix.CreateTranslation(TranslateX, TranslateY);

        using (dc.PushTransform(matrix))
        {
            if (Mode == ConnectionType.MutuallyExclusive)
            {
                FocusConnectionLinesControl.DrawMutuallyExclusive(dc, From, To);
            }

            if (Mode == ConnectionType.Prerequisite)
            {
                FocusConnectionLinesControl.DrawPrerequisiteLine(
                    dc,
                    From,
                    To,
                    FocusConnectionLinesControl.PrerequisiteLinePen
                );
            }

            if (Mode == ConnectionType.RelativePosition)
            {
                dc.DrawLine(
                    FocusConnectionLinesControl.PrerequisiteLinePen,
                    GetNodeCenterPoint(From),
                    GetNodeCenterPoint(To)
                );
            }
        }
    }

    public void ClearPreview()
    {
        From = null;
        To = null;
        Mode = ConnectionType.None;
    }

    private static Point GetNodeCenterPoint(FocusNode node)
    {
        return new Point(
            node.X * ProjectConfigService.FocusCellWidth + ProjectConfigService.FocusCellWidth / 2,
            node.Y * ProjectConfigService.FocusCellHeight + ProjectConfigService.FocusCellHeight / 2
        );
    }
}
