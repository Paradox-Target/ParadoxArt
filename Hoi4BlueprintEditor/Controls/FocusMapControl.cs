using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CommunityToolkit.Mvvm.Messaging;
using Hoi4BlueprintEditor.Extensions;
using Hoi4BlueprintEditor.Helpers;
using Hoi4BlueprintEditor.Messages;
using Hoi4BlueprintEditor.Models.Focus;
using Hoi4BlueprintEditor.ViewsModels;

namespace Hoi4BlueprintEditor.Controls;

public sealed class FocusMapControl : ItemsControl
{
    private static double CellWidth => FocusMapMetrics.CellWidth;
    private static double CellHeight => FocusMapMetrics.CellHeight;

    private const double LinePenWidth = 3.0;
    private static readonly Pen PrerequisiteLinePen = new(Colors.PaleGoldenrod.ToBrush(), LinePenWidth);
    private static readonly Pen ExclusiveLinePen = new(Colors.OrangeRed.ToBrush(), LinePenWidth);
    private static readonly Pen PrerequisiteDashPen =
        new(Colors.LightGray.ToBrush(), LinePenWidth)
        {
            DashStyle = new DashStyle { Offset = 0, Dashes = [1, 2] },
        };

    protected override void OnInitialized(EventArgs e)
    {
        base.OnInitialized(e);

        StrongReferenceMessenger.Default.Register<RedrawFocusLinkLinesMessage>(this, OnMoveFocus);
    }

    private void OnMoveFocus(object recipient, RedrawFocusLinkLinesMessage message)
    {
        InvalidateVisual();
    }

    protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
    {
        base.OnItemsChanged(e);

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
            DrawPrerequisite(dc, node);
            DrawMutuallyExclusive(dc, node);
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

    private static void DrawPrerequisite(DrawingContext dc, FocusNode node, FocusNode pre, Pen pen)
    {
        var offset = new Vector(0, CellHeight / 2);

        var nodeJoint = GetNodeCenter(node);
        var preJoint = GetNodeCenter(pre);
        var nodeCorner = Point.Subtract(nodeJoint, offset);
        var preCorner = Point.Add(preJoint, offset);

        var middleY = (nodeCorner.Y + preCorner.Y) / 2;
        nodeCorner.Y = preCorner.Y = middleY;

        dc.DrawLine(pen, nodeJoint, nodeCorner);
        dc.DrawLine(pen, preJoint, preCorner);
        dc.DrawLine(pen, nodeCorner, preCorner);
    }

    private static void DrawMutuallyExclusive(DrawingContext dc, FocusNode node)
    {
        foreach (var ex in node.MutuallyExclusive)
        {
            var nodeJoint = GetNodeCenter(node);
            var exJoint = GetNodeCenter(ex);
            dc.DrawLine(ExclusiveLinePen, nodeJoint, exJoint);
        }
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
