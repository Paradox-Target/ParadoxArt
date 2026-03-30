using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Messaging;
using Hoi4BlueprintBuilder.Core.Messages;
using Hoi4BlueprintBuilder.Core.Models.Focus;
using Hoi4BlueprintBuilder.Core.Services;
using Hoi4BlueprintBuilder.Core.ViewsModels;
using Microsoft.Extensions.DependencyInjection;
using ZLinq;

namespace Hoi4BlueprintBuilder.Core.Controls;

/// <summary>
/// 绘制国策节点之间连接线的控件
/// </summary>
public sealed class FocusConnectionLinesControl : Control
{
    private static double CellWidth => ProjectConfigService.FocusCellWidth;
    private static double CellHeight => ProjectConfigService.FocusCellHeight;

    private const double LinePenWidth = 3.0;
    public static readonly Pen PrerequisiteLinePen =
        new(new SolidColorBrush(Colors.PaleGoldenrod), LinePenWidth);
    private static readonly Pen ExclusiveLinePen = new(new SolidColorBrush(Colors.OrangeRed), LinePenWidth);

    /// 虚线
    private static readonly Pen PrerequisiteDashPen =
        new(new SolidColorBrush(Colors.LightGray), LinePenWidth, new DashStyle([1, 2], 0));

    #region Styled Properties

    public static readonly StyledProperty<IList<FocusNodeViewModel>?> NodesProperty =
        AvaloniaProperty.Register<FocusConnectionLinesControl, IList<FocusNodeViewModel>?>(nameof(Nodes));

    public IList<FocusNodeViewModel>? Nodes
    {
        get => GetValue(NodesProperty);
        set => SetValue(NodesProperty, value);
    }

    public static readonly StyledProperty<double> ScaleProperty = AvaloniaProperty.Register<
        FocusConnectionLinesControl,
        double
    >(nameof(Scale), defaultValue: 1.0);

    public double Scale
    {
        get => GetValue(ScaleProperty);
        set => SetValue(ScaleProperty, value);
    }

    public static readonly StyledProperty<double> TranslateXProperty = AvaloniaProperty.Register<
        FocusConnectionLinesControl,
        double
    >(nameof(TranslateX), defaultValue: 0.0);

    public double TranslateX
    {
        get => GetValue(TranslateXProperty);
        set => SetValue(TranslateXProperty, value);
    }

    public static readonly StyledProperty<double> TranslateYProperty = AvaloniaProperty.Register<
        FocusConnectionLinesControl,
        double
    >(nameof(TranslateY), defaultValue: 0.0);

    public double TranslateY
    {
        get => GetValue(TranslateYProperty);
        set => SetValue(TranslateYProperty, value);
    }

    private static readonly ProjectConfigService ProjectConfigService =
        App.Current.Services.GetRequiredService<ProjectConfigService>();

    static FocusConnectionLinesControl()
    {
        AffectsRender<FocusConnectionLinesControl>(
            NodesProperty,
            ScaleProperty,
            TranslateXProperty,
            TranslateYProperty
        );
    }

    #endregion

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property != NodesProperty)
        {
            return;
        }

        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        if (Nodes is null)
        {
            return;
        }

        // 应用与 ItemsControl 相同的变换: 先缩放后平移
        using var _ = context.PushTransform(new Matrix(Scale, 0, 0, Scale, TranslateX, TranslateY));

        DrawNodeConnectionsLines(context, Nodes);
    }

    public static void DrawNodeConnectionsLines(DrawingContext context, IEnumerable<FocusNodeViewModel> nodes)
    {
        foreach (var viewModel in nodes)
        {
            var node = viewModel.Node;

            if (!node.IsVisible)
            {
                continue;
            }

            if (node.Prerequisite.Count > 0)
            {
                DrawPrerequisite(context, node);
            }

            if (node.MutuallyExclusive.Count > 0)
            {
                DrawMutuallyExclusive(context, node);
            }
        }
    }

    private static void DrawPrerequisite(DrawingContext dc, FocusNode node)
    {
        foreach (var preGroup in node.Prerequisite)
        {
            if (preGroup.Count > 1)
            {
                foreach (var pre in preGroup)
                {
                    DrawPrerequisiteLine(dc, node, pre, PrerequisiteDashPen);
                }
            }
            else if (preGroup.Count == 1)
            {
                DrawPrerequisiteLine(dc, node, preGroup[0], PrerequisiteLinePen);
            }
        }
    }

    public static void DrawPrerequisiteLine(DrawingContext dc, FocusNode node, FocusNode pre, IPen pen)
    {
        if (!pre.IsVisible || !node.IsVisible)
        {
            return;
        }

        var offset = new Vector(0, CellHeight / 2);

        var nodeJoint = GetNodeCenter(node);
        var preJoint = GetNodeCenter(pre);
        var nodeCorner = nodeJoint - offset;
        var preCorner = preJoint + offset;

        var middleY = (nodeCorner.Y + preCorner.Y) / 2;
        nodeCorner = nodeCorner.WithY(middleY);
        preCorner = preCorner.WithY(middleY);

        dc.DrawLine(pen, nodeJoint, nodeCorner);
        dc.DrawLine(pen, preJoint, preCorner);
        dc.DrawLine(pen, nodeCorner, preCorner);
    }

    private static void DrawMutuallyExclusive(DrawingContext dc, FocusNode node)
    {
        foreach (var ex in node.MutuallyExclusive.AsValueEnumerable().Where(focusNode => focusNode.IsVisible))
        {
            DrawMutuallyExclusive(dc, node, ex);
        }
    }

    public static void DrawMutuallyExclusive(DrawingContext dc, FocusNode node, FocusNode secondNode)
    {
        var nodeJoint = GetNodeCenter(node);
        var exJoint = GetNodeCenter(secondNode);
        dc.DrawLine(ExclusiveLinePen, nodeJoint, exJoint);
    }

    private static Point GetNodeCenter(FocusNode node)
    {
        double x = node.X * CellWidth;
        double y = node.Y * CellHeight;
        x += (CellWidth / 2) - (LinePenWidth / 2);
        y += (CellHeight / 2) - (LinePenWidth / 2);
        return new Point(x, y);
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        StrongReferenceMessenger.Default.UnregisterAll(this);

        base.OnUnloaded(e);
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        StrongReferenceMessenger.Default.Register<RedrawFocusConnectionLinesMessage>(this, Handler);
    }

    private void Handler(object o, RedrawFocusConnectionLinesMessage redrawFocusConnectionLinesMessage)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            InvalidateVisual();
        }
        else
        {
            Dispatcher.UIThread.Post(InvalidateVisual);
        }
    }
}
