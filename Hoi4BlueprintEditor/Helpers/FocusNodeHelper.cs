using Hoi4BlueprintEditor.Extensions;
using Hoi4BlueprintEditor.Models.Focus;
using Hoi4BlueprintEditor.Services;
using MethodTimer;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using ParadoxPower.CSharpExtensions;
using ParadoxPower.Process;
using ZLinq;

namespace Hoi4BlueprintEditor.Helpers;

public static class FocusNodeHelper
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    // TODO: 看需不需要删
    private static readonly string[] FocusKeywords = [Keywords.Focus, "shared_focus"];

    [Time]
    public static Dictionary<string, FocusNode> GetAllNodesFromAst(Node rootNode)
    {
        var focusMap = new Dictionary<string, FocusNode>();

        //TODO: 遵守shared_focus的规则(?)
        var pathService = App.Current.Services.GetRequiredService<GameResourcesPathService>();
        var configs = GetConfigs(rootNode);
        foreach (var config in configs.AsValueEnumerable().Where(config => config.Key == "shared_focus"))
        {
            string? sharedFocusPath = pathService.GetFilePathPriorModByRelativePath(config.Value);
            if (sharedFocusPath is null)
            {
                Log.Warn("无效配置项, 共享国策文件路径未找到: {Path}", config.Value);
                continue;
            }

            if (TextParser.TryParse(sharedFocusPath, out var node, out _))
            {
                var map = GetAllNodesFromAst(node);
                foreach (var focusNode in map)
                {
                    focusMap[focusNode.Key] = focusNode.Value;
                }
            }
        }

        foreach (var focusNode in GetFocusNodesFromAstRootNode(rootNode))
        {
            var focusNodeModel = CreateFocusNodeFromAstNode(focusNode);
            focusMap[focusNodeModel.Id] = focusNodeModel;
        }

        ProcessFocusNodes(focusMap);

        return focusMap;
    }

    public static IEnumerable<Node> GetFocusNodesFromAstRootNode(Node rootNode)
    {
        var focusTreeNode = rootNode
            .Nodes.AsValueEnumerable()
            .FirstOrDefault(node => node.Key.EqualsIgnoreCase("focus_tree"));

        IEnumerable<Node>? nodes = null;
        if (focusTreeNode is not null)
        {
            nodes = focusTreeNode.Nodes.Where(node =>
                FocusKeywords.AsValueEnumerable().Any(keyword => keyword.EqualsIgnoreCase(node.Key))
            );
        }

        var sharedFocusNode = rootNode.Nodes.Where(node => node.Key.EqualsIgnoreCase("shared_focus"));
        nodes = nodes is null ? sharedFocusNode : nodes.Concat(sharedFocusNode);

        return nodes;
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

    private static Dictionary<string, string> GetConfigs(Node rootNode)
    {
        var configs = new Dictionary<string, string>();

        bool start = false;
        foreach (var comment in rootNode.Comments)
        {
            if (comment.Comment == "config:start")
            {
                start = true;
                continue;
            }
            if (comment.Comment == "config:end")
            {
                start = false;
                break;
            }
            if (start)
            {
                string[] parts = comment.Comment.Split(':', 2, StringSplitOptions.TrimEntries);
                configs[parts[0]] = parts[1];
            }
        }

        return configs;
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
