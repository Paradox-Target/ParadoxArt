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
    private static readonly Pen LinePen = new(Colors.Azure.ToBrush(), LinePenWidth);

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
                    var nodeJoint = GetChildNodeLineJoint(node);
                    var preJoint = GetPrerequisiteNodeLineJoint(pre);
                    dc.DrawLine(LinePen, nodeJoint, preJoint);
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
}
