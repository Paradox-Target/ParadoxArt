using Hoi4BlueprintEditor.Extensions;
using Hoi4BlueprintEditor.Models.Focus;
using MethodTimer;
using ParadoxPower.CSharpExtensions;
using ParadoxPower.Process;
using ZLinq;

namespace Hoi4BlueprintEditor.Core.Helpers;

public static class FocusNodeHelper
{
    [Time]
    public static IEnumerable<FocusNode> GetAllNodesFromAst(Node rootNode)
    {
        var focusMap = new Dictionary<string, FocusNode>();
        var focusTreeNode = rootNode
            .Nodes.AsValueEnumerable()
            .FirstOrDefault(node => node.Key.EqualsIgnoreCase("focus_tree"));

        foreach (
            var focusNode in focusTreeNode?.Nodes.Where(node => node.Key.EqualsIgnoreCase("focus")) ?? []
        )
        {
            var focusNodeModel = CreateFocusNodeFromAstNode(focusNode);
            focusMap.Add(focusNodeModel.Id, focusNodeModel);
        }

        foreach (var focusNode in focusMap.Values)
        {
            if (focusNode.RelativePosition is null)
            {
                continue;
            }

            // 如果找不到相对位置的节点，则设置为 null
            focusNode.RelativePosition = focusMap.GetValueOrDefault(focusNode.RelativePosition.Id);
        }

        return focusMap.Values;
    }

    private static FocusNode CreateFocusNodeFromAstNode(Node focusNode)
    {
        var model = new FocusNode();
        var point = new Point();

        foreach (var child in focusNode.AllArray)
        {
            if (child.TryGetLeaf(out var leaf))
            {
                if (leaf.Key.EqualsIgnoreCase("x"))
                {
                    point.X = leaf.Value.TryGetInt(out int x) ? x : 0;
                }
                else if (leaf.Key.EqualsIgnoreCase("y"))
                {
                    point.Y = leaf.Value.TryGetInt(out int y) ? y : 0;
                }
                else if (leaf.Key.EqualsIgnoreCase("id"))
                {
                    model.Id = leaf.ValueText;
                }
                else if (leaf.Key.EqualsIgnoreCase("icon"))
                {
                    model.Icon = leaf.ValueText;
                }
                else if (leaf.Key.EqualsIgnoreCase("cost"))
                {
                    model.Cost = leaf.Value.TryGetInt(out int cost) ? cost : 0;
                }
                else if (leaf.Key.EqualsIgnoreCase("relative_position_id"))
                {
                    model.RelativePosition = new FocusNode { Id = leaf.ValueText };
                }
            }
        }

        model.RawPosition = point;
        return model;
    }
}
