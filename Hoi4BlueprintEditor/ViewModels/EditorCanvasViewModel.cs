using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Hoi4BlueprintEditor.Extensions;
using Hoi4BlueprintEditor.Helpers;
using Hoi4BlueprintEditor.Messages;
using Hoi4BlueprintEditor.Models.Focus;
using MethodTimer;
using NLog;
using ObservableCollections;
using ParadoxPower.CSharpExtensions;
using ParadoxPower.Parser;
using ParadoxPower.Process;
using ZLinq;

namespace Hoi4BlueprintEditor.ViewModels;

public sealed partial class EditorCanvasViewModel : ObservableObject
{
    public NotifyCollectionChangedSynchronizedViewList<FocusNodeViewModel> Nodes =>
        _nodes.ToNotifyCollectionChanged();
    private readonly ObservableList<FocusNodeViewModel> _nodes = [];
    private Dictionary<string, FocusNode> _editorNodesMap = [];
    private string _currentFilePath = string.Empty;
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public EditorCanvasViewModel()
    {
        // 假数据测试
        LoadTestData();

        WeakReferenceMessenger.Default.Register<OpenFileMessage>(
            this,
            (_, message) =>
            {
                if (!TextParser.TryParse(message.FilePath, out var rootNode, out var _))
                {
                    return;
                }

                _currentFilePath = message.FilePath;
                _nodes.Clear();
                _editorNodesMap = FocusNodeHelper.GetAllNodesFromAst(rootNode);
                _nodes.AddRange(_editorNodesMap.Select(pair => new FocusNodeViewModel(pair.Value)));
                Log.Info("已加载国策树文件: {FilePath}", message.FilePath);
                Log.Info("共添加: {Amount}", _nodes.Count);
            }
        );

        WeakReferenceMessenger.Default.Register<SaveFocusTreeMessage>(this, SaveFocusTree);
    }

    [Time]
    private void SaveFocusTree(object recipient, SaveFocusTreeMessage message)
    {
        if (_editorNodesMap.Count == 0)
        {
            Log.Info("没有国策树数据可供保存");
            return;
        }

        if (!TextParser.TryParse(_currentFilePath, out var rootNode, out _))
        {
            return;
        }

        var focusTreeNode = rootNode
            .Nodes.AsValueEnumerable()
            .FirstOrDefault(node => node.Key.EqualsIgnoreCase("focus_tree"));

        if (focusTreeNode is null)
        {
            Log.Warn("无法找到 focus_tree 节点，无法保存国策树");
            return;
        }

        var removedFocus = new List<Node>();

        foreach (var node in FocusNodeHelper.GetFocusNodesFromAstRootNode(rootNode))
        {
            if (_editorNodesMap.TryGetValue(node.Key, out var editorModel))
            {
                // 更新 AST 节点
                SyncContent(node, editorModel);
                _editorNodesMap.Remove(node.Key);
            }
            else
            {
                removedFocus.Add(node);
            }
        }

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
        foreach (var editorModel in _editorNodesMap.Values)
        {
            var focusNode = FocusNodeHelper.CreateAstNodeFromEditorModel(editorModel);
            children.Add(Child.Create(focusNode));
        }
        focusTreeNode.AllArray = children.ToArray();
    }

    private static void SyncContent(Node focusNode, FocusNode editorModel)
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
            .Where(child =>
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
            .Select(focus => ChildHelper.LeafString(Keywords.Focus, focus.Id))
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
                .Select(focus => ChildHelper.LeafString(Keywords.Focus, focus.Id))
                .ToArray();
            children.Add(ChildHelper.Node(Keywords.Prerequisite, prerequisiteChildren));
        }
    }

    [ObservableProperty]
    private double _scale = 1.0;

    [ObservableProperty]
    private double _translateX = 0;

    [ObservableProperty]
    private double _translateY = 0;

    private void LoadTestData()
    {
        _nodes.Add(
            new FocusNodeViewModel(
                new FocusNode
                {
                    Id = "GER_Test1",
                    RawPosition = new Point(0, 0),
                    Icon = "GFX_GER_Test1",
                }
            )
        );
        _nodes.Add(
            new FocusNodeViewModel(
                new FocusNode
                {
                    Id = "GER_Test2",
                    RawPosition = new Point(1, 0),
                    Icon = "GFX_GER_Test2",
                }
            )
        );
        _nodes.Add(
            new FocusNodeViewModel(
                new FocusNode
                {
                    Id = "GER_Test3",
                    RawPosition = new Point(2, 1),
                    Icon = "GFX_GER_Test3",
                }
            )
        );
        _nodes.Add(
            new FocusNodeViewModel(
                new FocusNode
                {
                    Id = "GER_Test4",
                    RawPosition = new Point(3, 1),
                    Icon = "GFX_GER_Test4",
                }
            )
        );
    }
}
