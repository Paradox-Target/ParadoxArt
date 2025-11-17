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

    private static readonly double LinePenWidth = 3.0;
    private static readonly Pen PrerequisiteLinePen = new(Colors.Azure.ToBrush(), LinePenWidth);
    private static readonly Pen ExclusiveLinePen = new(Colors.OrangeRed.ToBrush(), LinePenWidth);

    protected override void OnInitialized(EventArgs e)
    {
        base.OnInitialized(e);

        WeakReferenceMessenger.Default.Register<MoveFocusMessage>(this, OnMoveFocus);
    }

    private void OnMoveFocus(object recipient, MoveFocusMessage message)
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

            foreach (var preGroup in node.Prerequisite)
            {
                foreach (var pre in preGroup)
                {
                    var offset = new Vector(0, FocusMapMetrics.FocusCenterOffsetVertical);

                    var nodeJoint = GetChildNodeLineJoint(node);
                    var preJoint = GetPrerequisiteNodeLineJoint(pre);
                    var nodeCorner = Point.Subtract(nodeJoint, offset);
                    var preCorner = Point.Add(preJoint, offset);

                    var middleY = (nodeCorner.Y + preCorner.Y) / 2;
                    nodeCorner.Y = preCorner.Y = middleY;

                    dc.DrawLine(PrerequisiteLinePen, nodeJoint, nodeCorner);
                    dc.DrawLine(PrerequisiteLinePen, preJoint, preCorner);
                    dc.DrawLine(PrerequisiteLinePen, nodeCorner, preCorner);
                }
            }

            foreach (var ex in node.MutuallyExclusive)
            {
                if (node.X < ex.X)
                {
                    var nodeJoint = GetLeftExclusiveLineJoint(node);
                    var exJoint = GetRightExclusiveLineJoint(ex);
                    dc.DrawLine(ExclusiveLinePen, nodeJoint, exJoint);
                }
                else if (node.X > ex.X)
                {
                    var nodeJoint = GetRightExclusiveLineJoint(node);
                    var exJoint = GetLeftExclusiveLineJoint(ex);
                    dc.DrawLine(ExclusiveLinePen, nodeJoint, exJoint);
                }
            }
        }
    }

    private Point GetChildNodeLineJoint(FocusNode node)
    {
        var x = node.X * CellWidth;
        var y = node.Y * CellHeight;
        x += CellWidth / 2 - LinePenWidth / 2;
        y += FocusMapMetrics.FocusCenterOffsetVertical;
        return new Point(x, y);
    }

    private Point GetPrerequisiteNodeLineJoint(FocusNode node)
    {
        var x = node.X * CellWidth;
        var y = node.Y * CellHeight;
        x += CellWidth / 2 - LinePenWidth / 2;
        y += CellHeight;
        y -= FocusMapMetrics.FocusCenterOffsetVertical;
        return new Point(x, y);
    }

    private Point GetLeftExclusiveLineJoint(FocusNode node)
    {
        var x = node.X * CellWidth;
        var y = node.Y * CellHeight;
        x += CellWidth;
        y += CellHeight / 2 - LinePenWidth / 2;
        return new Point(x, y);
    }

    private Point GetRightExclusiveLineJoint(FocusNode node)
    {
        var x = node.X * CellWidth;
        var y = node.Y * CellHeight;
        y += CellHeight / 2 - LinePenWidth / 2;
        return new Point(x, y);
    }
}
