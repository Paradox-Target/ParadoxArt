using Hoi4BlueprintEditor.Extensions;
using Hoi4BlueprintEditor.Models.Focus;
using ParadoxPower.CSharpExtensions;
using ParadoxPower.Parser;
using ParadoxPower.Process;
using ZLinq;

namespace Hoi4BlueprintEditor.Helpers;

public static class NodeHelper
{
    /// <summary>
    /// 同步 <c>focusTreeNode</c> 节点的子节点
    /// </summary>
    public static void SyncNodeChildren(
        Node focusTreeNode,
        List<Node> removedFocus,
        Dictionary<string, FocusNode> editorNodesMap,
        FocusType syncFocusType
    )
    {
        var children = focusTreeNode.AllArray.ToList();
        // 删除编辑器中不存在的节点
        foreach (var node in removedFocus)
        {
            for (int i = 0; i < children.Count; i++)
            {
                var child = children[i];
                if (child.TryGetNode(out var existingNode) && existingNode.Position == node.Position)
                {
                    children.RemoveAt(i);
                    break;
                }
            }
        }

        // 添加新增的节点
        foreach (
            var editorModel in editorNodesMap
                .Values.AsValueEnumerable()
                .Where(focus => focus.Type == syncFocusType)
        )
        {
            var focusNode = FocusNodeHelper.CreateAstNodeFromEditorModel(editorModel);
            children.Add(focusNode);
            editorNodesMap.Remove(editorModel.Id);
        }
        focusTreeNode.AllArray = children.ToArray();
    }

    public static void SyncNodeContent(Node focusNode, FocusNode editorModel)
    {
        SyncLeafContent(focusNode, editorModel);

        var children = GetFilteredChildren(focusNode);

        AddMutuallyExclusiveToChildrenIfExist(children, editorModel);

        AddPrerequisiteToChildrenIfExist(children, editorModel);

        if (editorModel.RelativePosition is not null)
        {
            children.Add(
                ChildHelper.LeafString(Keywords.RelativePositionId, editorModel.RelativePosition.Id)
            );
        }

        focusNode.AllArray = children.ToArray();
    }

    private static void SyncLeafContent(Node focusNode, FocusNode editorModel)
    {
        // TODO: 不遍历直接写入到 Children 中性能是不是会更好?
        foreach (var leaf in focusNode.Leaves)
        {
            if (leaf.Key.EqualsIgnoreCase(Keywords.Cost))
            {
                leaf.Value = Types.Value.NewFloat(editorModel.Cost);
            }
            else if (leaf.Key.EqualsIgnoreCase("x"))
            {
                leaf.Value = Types.Value.NewInt(editorModel.RawPosition.X);
            }
            else if (leaf.Key.EqualsIgnoreCase("y"))
            {
                leaf.Value = Types.Value.NewInt(editorModel.RawPosition.Y);
            }
            else if (leaf.Key.EqualsIgnoreCase(Keywords.Icon))
            {
                leaf.Value = Types.Value.NewString(editorModel.Icon);
            }
        }
    }

    private static List<Child> GetFilteredChildren(Node focusNode)
    {
        return focusNode
            .AllArray.AsValueEnumerable()
            .Where(static child =>
            {
                // 排除掉不需要的 MutuallyExclusive, Prerequisite, RelativePositionId
                // 这些内容完全按照编辑器模型保存
                if (
                    child.TryGetNode(out var node)
                    && (
                        node.Key.EqualsIgnoreCase(Keywords.MutuallyExclusive)
                        || node.Key.EqualsIgnoreCase(Keywords.Prerequisite)
                    )
                )
                {
                    return false;
                }

                if (child.TryGetLeaf(out var leaf) && leaf.Key.EqualsIgnoreCase(Keywords.RelativePositionId))
                {
                    return false;
                }

                return true;
            })
            .ToList();
    }

    private static void AddMutuallyExclusiveToChildrenIfExist(List<Child> children, FocusNode editorModel)
    {
        if (editorModel.MutuallyExclusive.Count == 0)
        {
            return;
        }

        var mutuallyExclusive = editorModel
            .MutuallyExclusive.AsValueEnumerable()
            .Select(static focus => ChildHelper.LeafString(Keywords.Focus, focus.Id))
            .ToArray();
        var mutuallyExclusiveChild = ChildHelper.Node(Keywords.MutuallyExclusive, mutuallyExclusive);
        children.Add(mutuallyExclusiveChild);
    }

    private static void AddPrerequisiteToChildrenIfExist(List<Child> children, FocusNode editorModel)
    {
        if (editorModel.Prerequisite.Count == 0)
        {
            return;
        }

        foreach (var prerequisite in editorModel.Prerequisite)
        {
            var prerequisiteChildren = prerequisite
                .AsValueEnumerable()
                .Select(static focus => ChildHelper.LeafString(Keywords.Focus, focus.Id))
                .ToArray();
            children.Add(ChildHelper.Node(Keywords.Prerequisite, prerequisiteChildren));
        }
    }
}
