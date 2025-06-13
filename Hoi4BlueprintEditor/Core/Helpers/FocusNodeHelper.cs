using Hoi4BlueprintEditor.Extensions;
using Hoi4BlueprintEditor.Models.Focus;
using MethodTimer;
using NLog;
using ParadoxPower.CSharpExtensions;
using ParadoxPower.Process;
using ZLinq;

namespace Hoi4BlueprintEditor.Core.Helpers;

public static class FocusNodeHelper
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

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
            if (focusNode.RelativePosition is not null)
            {
                // 如果找不到相对位置的节点，则设置为 null
                focusNode.RelativePosition = focusMap.GetValueOrDefault(focusNode.RelativePosition.Id);
            }

            if (focusNode.MutuallyExclusive.Count != 0)
            {
                foreach (var focusNodeMutuallyExclusive in focusNode.MutuallyExclusive.ToArray())
                {
                    focusNode.MutuallyExclusive.Remove(focusNodeMutuallyExclusive);
                    if (focusMap.TryGetValue(focusNodeMutuallyExclusive.Id, out var node))
                    {
                        focusNode.MutuallyExclusive.Add(node);
                    }
                }
            }
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
                    model.Cost = leaf.Value.TryGetDecimal(out decimal cost) ? cost : 0;
                }
                else if (leaf.Key.EqualsIgnoreCase("relative_position_id"))
                {
                    model.RelativePosition = new FocusNode { Id = leaf.ValueText };
                }
            }
            else if (child.TryGetNode(out var node))
            {
                if (node.Key.EqualsIgnoreCase("mutually_exclusive"))
                {
                    foreach (var focusLeaf in node.Leaves)
                    {
                        model.MutuallyExclusive.Add(new FocusNode { Id = focusLeaf.ValueText });
                    }
                }
            }
        }

        model.RawPosition = point;
        return model;
    }
}
