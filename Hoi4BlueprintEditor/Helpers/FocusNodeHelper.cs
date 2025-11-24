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
    private static readonly GameResourcesPathService PathService =
        App.Current.Services.GetRequiredService<GameResourcesPathService>();

    /// <summary>
    /// 获取所有国策内容, 包含被链接的其他文件的内容
    /// </summary>
    /// <param name="filePath"></param>
    /// <param name="rootNode"></param>
    /// <returns>FilePaths是所有被加载的文件路径</returns>
    [Time("解析国策树")]
    public static (Dictionary<string, FocusNode> Nodes, IEnumerable<string> FilePaths) GetAllNodesFromAst(
        string filePath,
        Node rootNode
    )
    {
        var focusMap = new Dictionary<string, FocusNode>();
        HashSet<string> filePaths = [filePath];

        GetAllNodesFromAstCore(filePath, rootNode, filePaths, focusMap);

        return (focusMap, filePaths);
    }

    private static void GetAllNodesFromAstCore(
        string filePath,
        Node rootNode,
        HashSet<string> filePaths,
        Dictionary<string, FocusNode> focusMap
    )
    {
        //TODO: 遵守shared_focus的规则(?)
        var configs = GetConfigs(rootNode);
        foreach (
            var config in configs
                .AsValueEnumerable()
                .Where(static config => config.Key == Keywords.SharedFocus)
        )
        {
            string? sharedFocusPath = PathService.GetFilePathPriorModByRelativePath(config.Value);
            if (sharedFocusPath is null)
            {
                Log.Warn("无效配置项, 共享国策文件路径未找到: {Path}", config.Value);
                continue;
            }

            if (TextParser.TryParse(sharedFocusPath, out var node, out _))
            {
                filePaths.Add(sharedFocusPath);
                GetAllNodesFromAstCore(sharedFocusPath, node, filePaths, focusMap);
            }
        }

        foreach (var focusNode in GetFocusNodesFromAstRootNode(rootNode))
        {
            var focusNodeModel = CreateFocusNodeFromAstNode(filePath, focusNode);
            focusMap[focusNodeModel.Id] = focusNodeModel;
        }

        ProcessFocusNodes(focusMap);
    }

    /// <summary>
    /// 获取所有国策 <see cref="Node"/>, 包括普通国策和共享国策。
    /// </summary>
    /// <param name="rootNode">根节点</param>
    /// <returns></returns>
    public static IEnumerable<Node> GetFocusNodesFromAstRootNode(Node rootNode)
    {
        var focusTreeNode = rootNode
            .Nodes.AsValueEnumerable()
            .FirstOrDefault(static node => node.Key.EqualsIgnoreCase("focus_tree"));

        IEnumerable<Node>? nodes = null;
        if (focusTreeNode is not null)
        {
            nodes = focusTreeNode.Nodes.Where(static node => node.Key.EqualsIgnoreCase(Keywords.Focus));
        }

        var sharedFocusNode = rootNode.Nodes.Where(static node =>
            node.Key.EqualsIgnoreCase(Keywords.SharedFocus)
        );
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

    /// <summary>
    /// 获取配置信息
    /// </summary>
    /// <param name="rootNode">文档根节点</param>
    /// <returns>返回键值对，键为配置项，值为配置项的值</returns>
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
        var newPrerequisites = new List<List<FocusNode>>();

        foreach (var prerequisiteList in focusNode.Prerequisite)
        {
            var newGroup = new List<FocusNode>(prerequisiteList.Count);
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

    private static FocusNode CreateFocusNodeFromAstNode(string filePath, Node focusNode)
    {
        var model = new FocusNode(filePath, GetFocusType(focusNode));

        int x = 0;
        int y = 0;
        foreach (var child in focusNode.AllArray)
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
                    model.RelativePosition = new FocusNode(string.Empty, FocusType.Normal)
                    {
                        Id = leaf.ValueText
                    };
                }
            }
            else if (child.TryGetNode(out var node))
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
                    var prerequisite = node
                        .Leaves.AsValueEnumerable()
                        .Select(static nodeLeaf => new FocusNode(string.Empty, FocusType.Normal)
                        {
                            Id = nodeLeaf.ValueText
                        })
                        .ToList();
                    if (prerequisite.Count != 0)
                    {
                        model.AddPrerequisite(prerequisite);
                    }
                }
            }
        }

        model.RawPosition = new FocusPoint(x, y);
        return model;
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

    public static Node CreateAstNodeFromEditorModel(FocusNode editorModel)
    {
        var children = new List<Child>(16)
        {
            ChildHelper.LeafString("id", editorModel.Id),
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

        if (editorModel.MutuallyExclusive.Count != 0)
        {
            children.Add(
                ChildHelper.Node(
                    Keywords.MutuallyExclusive,
                    editorModel.MutuallyExclusive.Select(static focus =>
                        ChildHelper.LeafString(Keywords.Focus, focus.Id)
                    )
                )
            );
        }

        foreach (var prerequisite in editorModel.Prerequisite)
        {
            var prerequisiteNode = ChildHelper.Node(
                Keywords.Prerequisite,
                prerequisite.Select(static focus => ChildHelper.LeafString(Keywords.Focus, focus.Id))
            );
            children.Add(prerequisiteNode);
        }

        var focusNode = new Node(editorModel.Type == FocusType.Shared ? Keywords.SharedFocus : Keywords.Focus)
        {
            AllArray = children.ToArray()
        };
        return focusNode;
    }
}
