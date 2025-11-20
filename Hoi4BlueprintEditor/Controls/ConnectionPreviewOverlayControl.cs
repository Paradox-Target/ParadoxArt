using System.Windows;
using System.Windows.Media;
using Hoi4BlueprintEditor.Models.Focus;
using Hoi4BlueprintEditor.Views;

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

    public EditorCanvasView.ConnectionType State { get; set; }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        if (From is null || To is null || State == EditorCanvasView.ConnectionType.None)
        {
            return;
        }

        var matrix = new Matrix();
        matrix.Scale(Scale, Scale);
        matrix.Translate(TranslateX, TranslateY);
        dc.PushTransform(new MatrixTransform(matrix));

        if (State == EditorCanvasView.ConnectionType.MutuallyExclusive)
        {
            FocusMapControl.DrawMutuallyExclusive(dc, From, To);
        }

        if (State == EditorCanvasView.ConnectionType.Prerequisite)
        {
            FocusMapControl.DrawPrerequisite(dc, From, To, FocusMapControl.PrerequisiteLinePen);
        }
        dc.Pop();
    }

    public void ClearPreview()
    {
        From = null;
        To = null;
        State = EditorCanvasView.ConnectionType.None;
    }
}
