using System.Windows;
using System.Windows.Media;
using Hoi4BlueprintEditor.Constants;
using Hoi4BlueprintEditor.Models;
using Hoi4BlueprintEditor.Models.Focus;

namespace Hoi4BlueprintEditor.Controls;

public sealed class ConnectionPreviewOverlayControl : FrameworkElement
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

    public static readonly DependencyProperty ScaleProperty = DependencyProperty.Register(
        nameof(Scale),
        typeof(double),
        typeof(ConnectionPreviewOverlayControl),
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
        typeof(ConnectionPreviewOverlayControl),
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
        typeof(ConnectionPreviewOverlayControl),
        new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender)
    );

    public double TranslateY
    {
        get => (double)GetValue(TranslateYProperty);
        set => SetValue(TranslateYProperty, value);
    }

    public ConnectionType Mode { get; set; }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        if (From is null || To is null || Mode == ConnectionType.None)
        {
            return;
        }

        var matrix = new Matrix();
        matrix.Scale(Scale, Scale);
        matrix.Translate(TranslateX, TranslateY);
        dc.PushTransform(new MatrixTransform(matrix));

        if (Mode == ConnectionType.MutuallyExclusive)
        {
            FocusMapControl.DrawMutuallyExclusive(dc, From, To);
        }

        if (Mode == ConnectionType.Prerequisite)
        {
            FocusMapControl.DrawPrerequisite(dc, From, To, FocusMapControl.PrerequisiteLinePen);
        }

        if (Mode == ConnectionType.RelativePosition)
        {
            dc.DrawLine(
                FocusMapControl.PrerequisiteLinePen,
                GetNodeCenterPoint(From),
                GetNodeCenterPoint(To)
            );
        }
        dc.Pop();
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
            node.X * FocusMapConstants.CellWidth + FocusMapConstants.CellWidth / 2,
            node.Y * FocusMapConstants.CellHeight + FocusMapConstants.CellHeight / 2
        );
    }
}
