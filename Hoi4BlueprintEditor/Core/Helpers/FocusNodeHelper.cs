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
    private static readonly string[] FocusKeywords = [Keywords.Focus, "shared_focus"];

    [Time]
    public static Dictionary<string, FocusNode> GetAllNodesFromAst(Node rootNode)
    {
        var focusMap = new Dictionary<string, FocusNode>();

        var focusTreeNode = rootNode
            .Nodes.AsValueEnumerable()
            .FirstOrDefault(node => node.Key.EqualsIgnoreCase("focus_tree"));

        if (focusTreeNode is null)
        {
            return [];
        }

        foreach (var focusNode in GetFocusNodesFromAstRootNode(focusTreeNode))
        {
            var focusNodeModel = CreateFocusNodeFromAstNode(focusNode);
            focusMap[focusNodeModel.Id] = focusNodeModel;
        }

        ProcessFocusNodes(focusMap);

        return focusMap;
    }

    public static IEnumerable<Node> GetFocusNodesFromAstRootNode(Node focusTreeNode)
    {
        return focusTreeNode.Nodes.Where(node =>
            FocusKeywords.AsValueEnumerable().Any(keyword => keyword.EqualsIgnoreCase(node.Key))
        );
    }

    private static void ProcessFocusNodes(Dictionary<string, FocusNode> focusMap)
    {
        foreach (var focusNode in focusMap.Values)
        {
            if (focusNode.RelativePosition is not null)
            {
                // 如果找不到相对位置的节点，则设置为 null
                focusNode.RelativePosition = focusMap.GetValueOrDefault(focusNode.RelativePosition.Id);
            }

            if (focusNode.MutuallyExclusive.Count != 0)
            {
                ProcessMutuallyExclusive(focusNode, focusMap);
            }

            if (focusNode.Prerequisite.Count != 0)
            {
                ProcessPrerequisite(focusNode, focusMap);
            }
        }
    }

    private static void ProcessMutuallyExclusive(FocusNode focusNode, Dictionary<string, FocusNode> focusMap)
    {
        for (int index = focusNode.MutuallyExclusive.Count - 1; index >= 0; index--)
        {
            var focusNodeMutuallyExclusive = focusNode.MutuallyExclusive[index];
            if (focusMap.TryGetValue(focusNodeMutuallyExclusive.Id, out var node))
            {
                focusNode.MutuallyExclusive[index] = node;
            }
            else
            {
                focusNode.MutuallyExclusive.RemoveAt(index);
            }
        }
    }

    private static void ProcessPrerequisite(FocusNode focusNode, Dictionary<string, FocusNode> focusMap)
    {
        foreach (var prerequisiteList in focusNode.Prerequisite)
        {
            for (int i = prerequisiteList.Count - 1; i >= 0; i--)
            {
                var prerequisiteNode = prerequisiteList[i];
                if (focusMap.TryGetValue(prerequisiteNode.Id, out var node))
                {
                    prerequisiteList[i] = node;
                }
                else
                {
                    prerequisiteList.RemoveAt(i);
                }
            }
        }
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
                else if (leaf.Key.EqualsIgnoreCase(Keywords.Icon))
                {
                    model.Icon = leaf.ValueText;
                }
                else if (leaf.Key.EqualsIgnoreCase(Keywords.Cost))
                {
                    model.Cost = leaf.Value.TryGetDecimal(out decimal cost) ? cost : 0;
                }
                else if (leaf.Key.EqualsIgnoreCase(Keywords.RelativePositionId))
                {
                    model.RelativePosition = new FocusNode { Id = leaf.ValueText };
                }
            }
            else if (child.TryGetNode(out var node))
            {
                if (node.Key.EqualsIgnoreCase(Keywords.MutuallyExclusive))
                {
                    foreach (var focusLeaf in node.Leaves)
                    {
                        model.MutuallyExclusive.Add(new FocusNode { Id = focusLeaf.ValueText });
                    }
                }
                else if (node.Key.EqualsIgnoreCase(Keywords.Prerequisite))
                {
                    var prerequisite = node
                        .Leaves.AsValueEnumerable()
                        .Select(nodeLeaf => new FocusNode { Id = nodeLeaf.ValueText })
                        .ToList();
                    model.Prerequisite.Add(prerequisite);
                }
            }
        }

        model.RawPosition = point;
        return model;
    }

    public static Node CreateAstNodeFromEditorModel(FocusNode editorModel)
    {
        var children = new List<Child>(16)
        {
            ChildHelper.Leaf("x", editorModel.RawPosition.X),
            ChildHelper.Leaf("y", editorModel.RawPosition.Y),
            ChildHelper.LeafString(Keywords.Icon, editorModel.Icon),
            ChildHelper.Leaf(Keywords.Cost, editorModel.Cost)
        };

        if (editorModel.RelativePosition is not null)
        {
            children.Add(
                ChildHelper.LeafString(Keywords.RelativePositionId, editorModel.RelativePosition.Id)
            );
        }

        children.Add(
            ChildHelper.Node(
                Keywords.MutuallyExclusive,
                editorModel.MutuallyExclusive.Select(focus =>
                    ChildHelper.LeafString(Keywords.Focus, focus.Id)
                )
            )
        );

        foreach (var prerequisite in editorModel.Prerequisite)
        {
            var prerequisiteNode = ChildHelper.Node(
                Keywords.Prerequisite,
                prerequisite.Select(focus => ChildHelper.LeafString(Keywords.Focus, focus.Id))
            );
            children.Add(prerequisiteNode);
        }

        var focusNode = new Node(editorModel.Id) { AllArray = children.ToArray() };
        return focusNode;
    }
}
