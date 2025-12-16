using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using CommunityToolkit.Mvvm.Messaging;
using Hoi4BlueprintBuilder.Core.Constants;
using Hoi4BlueprintBuilder.Core.Messages;
using Hoi4BlueprintBuilder.Core.Models.Focus;
using Hoi4BlueprintBuilder.Core.ViewsModels;

namespace Hoi4BlueprintBuilder.Core.Controls;

public sealed class FocusMapControl : ItemsControl
{
    private static double CellWidth => FocusMapConstants.CellWidth;
    private static double CellHeight => FocusMapConstants.CellHeight;

    private const double LinePenWidth = 3.0;
    public static readonly Pen PrerequisiteLinePen = InitializePrerequisiteLinePen();
    private static readonly Pen ExclusiveLinePen = InitializeExclusiveLinePen();
    private static readonly Pen PrerequisiteDashPen = InitializePrerequisiteDashPen();

    private static Pen InitializePrerequisiteLinePen()
    {
        return new Pen(new SolidColorBrush(Colors.PaleGoldenrod), LinePenWidth);
    }

    private static Pen InitializeExclusiveLinePen()
    {
        return new Pen(new SolidColorBrush(Colors.OrangeRed), LinePenWidth);
    }

    private static Pen InitializePrerequisiteDashPen()
    {
        return new Pen(
            new SolidColorBrush(Colors.LightGray),
            LinePenWidth,
            new DashStyle([1, 2], 0)
        );
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();

        WeakReferenceMessenger.Default.Register<RedrawFocusConnectionLinesMessage>(
            this,
            OnMoveFocus
        );
        Items.CollectionChanged += (_, _) => InvalidateVisual();
    }

    private void OnMoveFocus(object recipient, RedrawFocusConnectionLinesMessage message)
    {
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        DrawNodeConnectionsLines(context, Items.Cast<FocusNodeViewModel>());
    }

    public static void DrawNodeConnectionsLines(
        DrawingContext dc,
        IEnumerable<FocusNodeViewModel> nodes
    )
    {
        foreach (var viewModel in nodes)
        {
            var node = viewModel.Model;
            if (node.Prerequisite.Count > 0)
            {
                DrawPrerequisite(dc, node);
            }

            if (node.MutuallyExclusive.Count > 0)
            {
                DrawMutuallyExclusive(dc, node);
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
                    DrawPrerequisite(dc, node, pre, PrerequisiteDashPen);
                }
            }
            else if (preGroup.Count == 1)
            {
                DrawPrerequisite(dc, node, preGroup[0], PrerequisiteLinePen);
            }
        }
    }

    public static void DrawPrerequisite(DrawingContext dc, FocusNode node, FocusNode pre, IPen pen)
    {
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
        foreach (var ex in node.MutuallyExclusive)
        {
            DrawMutuallyExclusive(dc, node, ex);
        }
    }

    public static void DrawMutuallyExclusive(
        DrawingContext dc,
        FocusNode node,
        FocusNode secondNode
    )
    {
        var nodeJoint = GetNodeCenter(node);
        var exJoint = GetNodeCenter(secondNode);
        dc.DrawLine(ExclusiveLinePen, nodeJoint, exJoint);
    }

    private static Point GetNodeCenter(FocusNode node)
    {
        var x = node.X * CellWidth;
        var y = node.Y * CellHeight;
        x += (CellWidth / 2) - (LinePenWidth / 2);
        y += (CellHeight / 2) - (LinePenWidth / 2);
        return new Point(x, y);
    }
}
