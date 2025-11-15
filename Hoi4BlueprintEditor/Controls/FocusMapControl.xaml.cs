using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Hoi4BlueprintEditor.Extensions;
using Hoi4BlueprintEditor.Models.Focus;
using Hoi4BlueprintEditor.Views;
using Hoi4BlueprintEditor.ViewsModels;
using Point = System.Windows.Point;

namespace Hoi4BlueprintEditor.Controls;

public sealed partial class FocusMapControl : ItemsControl
{
    #region Dependency Proerties (依赖属性)
    public static readonly DependencyProperty ScaleXProperty = DependencyProperty.Register(
        nameof(ScaleX),
        typeof(double),
        typeof(FocusMapControl),
        new PropertyMetadata(1.0, OnTransformed)
    );
    public double ScaleX
    {
        get => (double)GetValue(ScaleXProperty);
        set => SetValue(ScaleXProperty, value);
    }

    public static readonly DependencyProperty ScaleYProperty = DependencyProperty.Register(
        nameof(ScaleY),
        typeof(double),
        typeof(FocusMapControl),
        new PropertyMetadata(1.0, OnTransformed)
    );
    public double ScaleY
    {
        get => (double)GetValue(ScaleYProperty);
        set => SetValue(ScaleYProperty, value);
    }

    public static readonly DependencyProperty TranslateXProperty = DependencyProperty.Register(
        nameof(TranslateX),
        typeof(double),
        typeof(FocusMapControl),
        new PropertyMetadata(0.0, OnTransformed)
    );
    public double TranslateX
    {
        get => (double)GetValue(TranslateXProperty);
        set => SetValue(TranslateXProperty, value);
    }

    public static readonly DependencyProperty TranslateYProperty = DependencyProperty.Register(
        nameof(TranslateY),
        typeof(double),
        typeof(FocusMapControl),
        new PropertyMetadata(0.0, OnTransformed)
    );
    public double TranslateY
    {
        get => (double)GetValue(TranslateYProperty);
        set => SetValue(TranslateYProperty, value);
    }
    #endregion

    public static readonly double CellWidth;
    public static readonly double CellHeight;

    static FocusMapControl()
    {
        CellWidth = (double)App.Current.FindResource("FocusNodeWidth");
        CellHeight = (double)App.Current.FindResource("FocusNodeHeight");
    }

    private static readonly Pen LinePen = new(Colors.Azure.ToBrush(), 3.0);
    private Canvas Canvas { get; set; } = new();

    public FocusMapControl()
    {
        InitializeComponent();
    }

    private static void OnTransformed(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FocusMapControl control)
        {
            return;
        }
        var trans = Matrix.Identity;
        trans.Scale(control.ScaleX, control.ScaleY);
        trans.Translate(control.TranslateX, control.TranslateY);
        var transform = new MatrixTransform(trans);
        control.Canvas.RenderTransform = transform;
        control.InvalidateVisual();
    }

    private void UpdateTransform() { }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        var presenter = GetVisualChild<ItemsPresenter>(this);
        if (presenter == null)
        {
            throw new NullReferenceException("FocusMapControl 缺失 ItemsPresenter");
        }
        presenter.ApplyTemplate();
        Canvas = (Canvas)ItemsPanel.FindName("PartCanvas", presenter);
    }

    private static T? GetVisualChild<T>(DependencyObject parent)
        where T : Visual
    {
        var child = default(T);

        int numVisuals = VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < numVisuals; i++)
        {
            var visual = (Visual)VisualTreeHelper.GetChild(parent, i);
            child = visual as T ?? GetVisualChild<T>(visual);
            if (child != null)
            {
                break;
            }
        }
        return child;
    }

    protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
    {
        InvalidateVisual();
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        foreach (FocusNodeViewModel viewModel in Items)
        {
            var node = viewModel.Model;
            if (node.Prerequisite.Count == 0)
            {
                continue;
            }

            foreach (var preGroup in node.Prerequisite)
            {
                foreach (var pre in preGroup)
                {
                    // TODO: 变换使用矩阵
                    var p1 = new Point(
                        node.X * CellWidth * ScaleX + TranslateX,
                        node.Y * CellHeight * ScaleY + TranslateY
                    );
                    var p2 = new Point(
                        pre.X * CellWidth * ScaleX + TranslateX,
                        pre.Y * CellHeight * ScaleY + TranslateY
                    );
                    dc.DrawLine(LinePen, p1, p2);
                }
            }
        }
    }
}
