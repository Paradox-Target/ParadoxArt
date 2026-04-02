using System.Diagnostics;
using Avalonia.Collections;
using Hoi4BlueprintBuilder.Core.Extensions;
using Hoi4BlueprintBuilder.Core.Helpers;
using Hoi4BlueprintBuilder.Core.Models.Focus;
using Hoi4BlueprintBuilder.Core.Services.GameResources;
using MethodTimer;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using ParadoxPower.CSharpExtensions;
using ParadoxPower.Process;

namespace Hoi4BlueprintBuilder.Core.Services;

[RegisterSingleton<FocusTreeFileService>]
public sealed class FocusTreeFileService(IServiceProvider serviceProvider)
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// 获取所有国策内容, 包含可能分布在不同文件内的共享国策
    /// </summary>
    /// <param name="filePath"></param>
    /// <param name="rootNode"></param>
    /// <returns>FilePaths是所有被加载的文件路径, ConditionItems 是所有提取的叶子条件</returns>
    [Time("解析国策树")]
    public (
        Dictionary<string, FocusNode> Nodes,
        IEnumerable<string> FilePaths,
        List<ConditionItem> ConditionItems
    ) GetAllNodesFromAst(string filePath, Node rootNode)
    {
        var focusMap = new Dictionary<string, FocusNode>();
        HashSet<string> filePaths = [filePath];
        var conditionItems = new List<ConditionItem>();

        GetAllNodesFromAstCore(filePath, rootNode, filePaths, focusMap, conditionItems);

        return (focusMap, filePaths, conditionItems);
    }

    private SharedFocusService? _sharedNodes;

    private void GetAllNodesFromAstCore(
        string filePath,
        Node rootNode,
        HashSet<string> filePaths,
        Dictionary<string, FocusNode> focusMap,
        List<ConditionItem> conditionItems
    )
    {
        foreach (var focusNode in NodeHelper.GetFocusNodesFromAstRootNode(rootNode))
        {
            var focusNodeModel = CreateFocusNodeFromAstNode(filePath, focusNode, conditionItems);
            focusMap[focusNodeModel.Id] = focusNodeModel;
        }

        ProcessFocusNodes(focusMap);

        var focusIds = GetSharedFocusIds(rootNode);
        if (focusIds.Count > 0)
        {
            _sharedNodes ??= GetSharedFocuses();

            var sharedFocusMap = new Dictionary<string, FocusNode>();
            foreach (var pair in _sharedNodes.AllSharedFocuses)
            {
                foreach (var node in pair.Value)
                {
                    var focusNodeModel = CreateFocusNodeFromAstNode(pair.Key, node.Value, conditionItems);
                    sharedFocusMap[focusNodeModel.Id] = focusNodeModel;
                }
            }

            ProcessFocusNodes(sharedFocusMap);
            foreach (string id in focusIds)
            {
                if (!sharedFocusMap.TryGetValue(id, out var focusNode))
                {
                    continue;
                }

                filePaths.Add(focusNode.Path);
                // 所有相关文件的国策都要添加进去
                focusMap[focusNode.Id] = focusNode;
                CycleLoadFocus(focusNode, filePaths, focusMap);
            }
        }
    }

    private void CycleLoadFocus(
        FocusNode headFocus,
        HashSet<string> filePaths,
        Dictionary<string, FocusNode> focusMap
    )
    {
        Debug.Assert(_sharedNodes is not null);

        var visited = new HashSet<string> { headFocus.Id };
        var queue = new Queue<FocusNode>();

        foreach (var child in headFocus.RelativePositionChildren)
        {
            queue.Enqueue(child);
        }

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            if (!visited.Add(current.Id))
            {
                break;
            }

            filePaths.Add(current.Path);
            focusMap[current.Id] = current;

            foreach (var child in current.RelativePositionChildren)
            {
                queue.Enqueue(child);
            }
        }
    }

    private SharedFocusService GetSharedFocuses()
    {
        return serviceProvider.GetRequiredService<SharedFocusService>();
    }

    /// <summary>
    /// 建立国策之间的连接关系
    /// </summary>
    /// <param name="focusMap"></param>
    [Time("建立国策之间的连接关系")]
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

        // 当有 AllowBranch 时, 需要在解析完成后设置 IsVisible
        // 否则会导致只有设置 AllowBranch 的节点会隐藏
        // TODO: 建造器模式应该可以优化
        foreach (var focusNode in focusMap.Values)
        {
            focusNode.EndInitialization();
        }
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="rootNode">文档根节点</param>
    /// <returns>返回键值对，键为配置项，值为配置项的值</returns>
    private static List<string> GetSharedFocusIds(Node rootNode)
    {
        var focusTreeNode = rootNode.Nodes.FirstOrDefault(static node =>
            node.Key.EqualsIgnoreCase("focus_tree")
        );
        if (focusTreeNode is null)
        {
            return [];
        }

        var sharedFocusIds = new List<string>();
        foreach (
            var leaf in focusTreeNode.Leaves.Where(static leaf =>
                leaf.Key.EqualsIgnoreCase(Keywords.SharedFocus)
            )
        )
        {
            sharedFocusIds.Add(leaf.ValueText);
        }
        return sharedFocusIds;
    }

    private static void ProcessMutuallyExclusive(FocusNode focusNode, Dictionary<string, FocusNode> focusMap)
    {
        var newMutuallyExclusive = new List<FocusNode>(focusNode.MutuallyExclusive.Count);
        foreach (var focusNodeMutuallyExclusive in focusNode.MutuallyExclusive)
        {
            if (focusMap.TryGetValue(focusNodeMutuallyExclusive.Id, out var node))
            {
                newMutuallyExclusive.Add(node);
            }
        }

        focusNode.ClearMutuallyExclusive();
        foreach (var node in newMutuallyExclusive)
        {
            focusNode.AddMutuallyExclusive(node);
        }
    }

    private static void ProcessPrerequisite(FocusNode focusNode, Dictionary<string, FocusNode> focusMap)
    {
        var newPrerequisites = new AvaloniaList<AvaloniaList<FocusNode>>();

        foreach (var prerequisiteList in focusNode.Prerequisite)
        {
            var newGroup = new AvaloniaList<FocusNode>(prerequisiteList.Count);
            foreach (var prerequisiteNode in prerequisiteList)
            {
                if (focusMap.TryGetValue(prerequisiteNode.Id, out var node))
                {
                    newGroup.Add(node);
                }
            }

            if (newGroup.Count > 0)
            {
                newPrerequisites.Add(newGroup);
            }
        }

        focusNode.ClearPrerequisites();
        foreach (var group in newPrerequisites)
        {
            focusNode.AddPrerequisite(group);
        }
    }

    private static FocusNode CreateFocusNodeFromAstNode(
        string filePath,
        Node focusNode,
        List<ConditionItem> conditionItems
    )
    {
        var model = new FocusNode(filePath, GetFocusType(focusNode));

        int x = 0;
        int y = 0;
        foreach (var child in focusNode.AllArray)
        {
            if (child.TryGetLeaf(out var leaf))
            {
                ProcessLeaf(ref x, ref y, leaf, model);
            }
            else if (child.TryGetNode(out var node))
            {
                ProcessNode(node, model, conditionItems);
            }
        }

        model.RawPosition = new FocusPoint(x, y);
        return model;
    }

    private static void ProcessLeaf(ref int x, ref int y, Leaf leaf, FocusNode model)
    {
        if (leaf.Key.EqualsIgnoreCase("x"))
        {
            x = leaf.Value.TryGetInt(out int result) ? result : 0;
        }
        else if (leaf.Key.EqualsIgnoreCase("y"))
        {
            y = leaf.Value.TryGetInt(out int result) ? result : 0;
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
            if (!leaf.Value.TryGetDecimal(out decimal cost) && leaf.Value.TryGetInt(out int costInt))
            {
                cost = costInt;
            }
            model.Cost = cost;
        }
        else if (leaf.Key.EqualsIgnoreCase(Keywords.RelativePositionId))
        {
            model.RelativePosition = new FocusNode(string.Empty, FocusType.Normal) { Id = leaf.ValueText };
        }
        else if (
            leaf.Key.EqualsIgnoreCase(Keywords.CancelIfInvalid)
            && leaf.Value.TryGetBool(out bool cancelIfInvalid)
        )
        {
            model.CancelIfInvalid = cancelIfInvalid;
        }
        else if (
            leaf.Key.EqualsIgnoreCase(Keywords.ContinueIfInvalid)
            && leaf.Value.TryGetBool(out bool continueIfInvalid)
        )
        {
            model.ContinueIfInvalid = continueIfInvalid;
        }
    }

    private static void ProcessNode(Node node, FocusNode model, List<ConditionItem> conditionItems)
    {
        if (node.Key.EqualsIgnoreCase(Keywords.MutuallyExclusive))
        {
            foreach (var focusLeaf in node.Leaves)
            {
                model.AddMutuallyExclusive(
                    new FocusNode(string.Empty, FocusType.Normal) { Id = focusLeaf.ValueText }
                );
            }
        }
        else if (node.Key.EqualsIgnoreCase(Keywords.Prerequisite))
        {
            var prerequisite = node.Leaves.Select(static nodeLeaf => new FocusNode(
                string.Empty,
                FocusType.Normal
            )
            {
                Id = nodeLeaf.ValueText
            });
            var prerequisiteList = new AvaloniaList<FocusNode>(prerequisite);
            if (prerequisiteList.Count != 0)
            {
                model.AddPrerequisite(prerequisiteList);
            }
        }
        else if (node.Key.EqualsIgnoreCase(Keywords.CompletionReward))
        {
            model.CompletionReward = node.ToScript();
        }
        else if (node.Key.EqualsIgnoreCase("offset"))
        {
            int x = 0;
            int y = 0;
            Node? trigger = null;
            foreach (var child in node.AllArray)
            {
                if (child.TryGetLeaf(out var leaf))
                {
                    if (leaf.Key.EqualsIgnoreCase("x"))
                    {
                        x = leaf.Value.TryGetInt(out int result) ? result : 0;
                    }
                    else if (leaf.Key.EqualsIgnoreCase("y"))
                    {
                        y = leaf.Value.TryGetInt(out int result) ? result : 0;
                    }
                }
                else if (child.TryGetNode(out var childNode) && childNode.Key.EqualsIgnoreCase("trigger"))
                {
                    trigger = childNode;
                }
            }

            var expression = trigger is not null
                ? ConditionHelper.ExtractConditionExpression(trigger, string.Empty, conditionItems)
                : null;
            model.AddOffset(new FocusOffset(new FocusPoint(x, y), expression));
        }
        else if (node.Key.EqualsIgnoreCase("allow_branch") && node.AllArray.Length != 0)
        {
            var expression = ConditionHelper.ExtractConditionExpression(node, string.Empty, conditionItems);
            model.AllowBranch = new FocusAllowBranch(expression);
        }
    }

    private static FocusType GetFocusType(Node focusNode)
    {
        return focusNode.Key switch
        {
            Keywords.Focus => FocusType.Normal,
            Keywords.SharedFocus => FocusType.Shared,
            _ => FocusType.Unknown
        };
    }
}
