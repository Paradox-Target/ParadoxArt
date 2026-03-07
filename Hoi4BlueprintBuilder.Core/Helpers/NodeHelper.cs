using Hoi4BlueprintBuilder.Core.Extensions;
using Hoi4BlueprintBuilder.Core.Models.Focus;
using NLog;
using ParadoxPower.CSharpExtensions;
using ParadoxPower.Process;
using ZLinq;

namespace Hoi4BlueprintBuilder.Core.Helpers;

public static class NodeHelper
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

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

    /// <summary>
    /// 在不改变其他内容的情况下, 同步编辑器支持修改的内容到 AST 节点中
    /// </summary>
    /// <param name="focusNode"></param>
    /// <param name="editorModel"></param>
    public static void SyncNodeContent(Node focusNode, FocusNode editorModel)
    {
        // TODO: 修改现有国策节点和生成新国策节点有很多重复的代码, 需要重构
        var children = new List<Child>(16)
        {
            ChildHelper.LeafString("id", editorModel.Id),
            ChildHelper.Leaf("x", editorModel.RawPosition.X),
            ChildHelper.Leaf("y", editorModel.RawPosition.Y),
            ChildHelper.Leaf(Keywords.Cost, editorModel.Cost)
        };

        if (!string.IsNullOrWhiteSpace(editorModel.Icon))
        {
            children.Add(ChildHelper.LeafString(Keywords.Icon, editorModel.Icon));
        }

        // TODO: 只有与默认值不同时才添加
        children.Add(ChildHelper.Leaf(Keywords.ContinueIfInvalid, editorModel.ContinueIfInvalid));
        children.Add(ChildHelper.Leaf(Keywords.CancelIfInvalid, editorModel.CancelIfInvalid));

        children.AddRange(GetFilteredChildren(focusNode));

        AddMutuallyExclusiveToChildrenIfExist(children, editorModel);

        AddPrerequisiteToChildrenIfExist(children, editorModel);

        AddCompletionRewardToChildrenIfExist(children, editorModel);

        if (editorModel.RelativePosition is not null)
        {
            children.Add(
                ChildHelper.LeafString(Keywords.RelativePositionId, editorModel.RelativePosition.Id)
            );
        }

        focusNode.AllArray = children.ToArray();
    }

    public static void AddCompletionRewardToChildrenIfExist(List<Child> children, FocusNode editorModel)
    {
        if (string.IsNullOrWhiteSpace(editorModel.CompletionReward))
        {
            return;
        }

        if (TextParser.TryParse(string.Empty, editorModel.CompletionReward, out var node, out var error))
        {
            var completionRewardNode = ChildHelper.Node(Keywords.CompletionReward, node.AllArray);
            children.Add(completionRewardNode);
        }
        else
        {
            Log.Warn("解析完成效果失败, 无法插入到AST树中. {Message}", error.ErrorMessage);
        }
    }

    private static IEnumerable<Child> GetFilteredChildren(Node focusNode)
    {
        return focusNode.AllArray.Where(static child =>
        {
            // 排除掉不需要的 MutuallyExclusive, Prerequisite, RelativePositionId, CompletionReward
            // 这些内容完全按照编辑器模型保存
            if (
                child.TryGetNode(out var node)
                && (
                    node.Key.EqualsIgnoreCase(Keywords.MutuallyExclusive)
                    || node.Key.EqualsIgnoreCase(Keywords.Prerequisite)
                    || node.Key.EqualsIgnoreCase(Keywords.CompletionReward)
                )
            )
            {
                return false;
            }

            if (
                child.TryGetLeaf(out var leaf)
                && (
                    leaf.Key.EqualsIgnoreCase(Keywords.RelativePositionId)
                    || leaf.Key.EqualsIgnoreCase("id")
                    || leaf.Key.EqualsIgnoreCase("x")
                    || leaf.Key.EqualsIgnoreCase("y")
                    || leaf.Key.EqualsIgnoreCase(Keywords.Cost)
                    || leaf.Key.EqualsIgnoreCase(Keywords.Icon)
                    || leaf.Key.EqualsIgnoreCase(Keywords.ContinueIfInvalid)
                    || leaf.Key.EqualsIgnoreCase(Keywords.CancelIfInvalid)
                )
            )
            {
                return false;
            }

            return true;
        });
    }

    public static void AddMutuallyExclusiveToChildrenIfExist(List<Child> children, FocusNode editorModel)
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

    public static void AddPrerequisiteToChildrenIfExist(List<Child> children, FocusNode editorModel)
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
