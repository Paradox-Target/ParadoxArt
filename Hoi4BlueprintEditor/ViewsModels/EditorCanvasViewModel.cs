using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Hoi4BlueprintEditor.Extensions;
using Hoi4BlueprintEditor.Helpers;
using Hoi4BlueprintEditor.Messages;
using Hoi4BlueprintEditor.Models.Focus;
using Hoi4BlueprintEditor.Services;
using MethodTimer;
using NLog;
using ObservableCollections;
using ParadoxPower.CSharpExtensions;
using ParadoxPower.Parser;
using ParadoxPower.Process;
using ZLinq;

namespace Hoi4BlueprintEditor.ViewsModels;

public sealed partial class EditorCanvasViewModel : ObservableObject
{
    public NotifyCollectionChangedSynchronizedViewList<FocusNodeViewModel> Nodes { get; }

    private readonly ObservableList<FocusNodeViewModel> _nodes = [];

    /// <summary>
    /// Key: FocusNode.Id, Value: FocusNode
    /// </summary>
    private Dictionary<string, FocusNode> _editorNodesMap = [];
    private readonly List<string> _focusTreeFiles = [];
    private readonly GameResourcesPathService _pathService;
    private readonly SettingsService _settingsService;
    private readonly NotificationService _notificationService;

    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public EditorCanvasViewModel(
        GameResourcesPathService pathService,
        SettingsService settingsService,
        NotificationService notificationService
    )
    {
        _pathService = pathService;
        _settingsService = settingsService;
        _notificationService = notificationService;
        Nodes = _nodes.ToNotifyCollectionChanged();
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

                ClearResources();

                var (focusNodes, filePaths) = FocusNodeHelper.GetAllNodesFromAst(message.FilePath, rootNode);
                _editorNodesMap = focusNodes;
                _focusTreeFiles.AddRange(filePaths);
                _nodes.AddRange(
                    _editorNodesMap.Values.Select(static focusNode => new FocusNodeViewModel(focusNode))
                );
                Log.Info("已加载国策树文件: {FilePath}", message.FilePath);
                Log.Info("共添加: {Amount}, 来自 {Count} 个文件", _nodes.Count, _focusTreeFiles.Count);
            }
        );

        WeakReferenceMessenger.Default.Register<SaveFocusTreeMessage>(this, SaveFocusTree);
        WeakReferenceMessenger.Default.Register<CreateNewFocusMessage>(
            this,
            (_, message) =>
            {
                message.Reply(
                    Task.Run(() =>
                    {
                        var focus = new FocusNode("", FocusType.Normal)
                        {
                            RawPosition = message.Position,
                            Id = GetNextFocusId()
                        };
                        App.Current.Dispatcher.Invoke(() =>
                        {
                            _nodes.Add(new FocusNodeViewModel(focus));
                        });
                        _editorNodesMap[focus.Id] = focus;
                        return focus;
                    })
                );
            }
        );
    }

    private static int _focusId = 1;

    private string GetNextFocusId()
    {
        string id;
        do
        {
            id = $"new_focus_{_focusId++}";
        } while (_editorNodesMap.ContainsKey(id));

        return id;
    }

    private void ClearResources()
    {
        foreach (var node in _nodes)
        {
            node.Dispose();
        }
        _nodes.Clear();
        _focusTreeFiles.Clear();
    }

    [Time]
    private void SaveFocusTree(object recipient, SaveFocusTreeMessage message)
    {
        if (_editorNodesMap.Count == 0)
        {
            Log.Info("没有国策树数据可供保存");
            return;
        }

        // 将编辑器中的 FocusNode 按照文件路径分组
        var maps = _editorNodesMap
            .AsValueEnumerable()
            .GroupBy(static pair => pair.Value.Path)
            .ToDictionary(
                static item => item.Key,
                static item =>
                    item.AsValueEnumerable().ToDictionary(static pair => pair.Key, static pair => pair.Value)
            );

        foreach (string filePath in _focusTreeFiles)
        {
            Debug.Assert(maps.ContainsKey(filePath));

            Save(filePath, maps[filePath]);
            Log.Debug("已保存国策树文件: {FilePath}", filePath);
        }

        _notificationService.Show("成功保存国策树");
    }

    private void Save(string filePath, Dictionary<string, FocusNode> editorNodesMap)
    {
        if (!TextParser.TryParse(filePath, out var rootNode, out _))
        {
            return;
        }

        var focusTreeNode = rootNode
            .Nodes.AsValueEnumerable()
            .FirstOrDefault(static node => node.Key.EqualsIgnoreCase("focus_tree"));

        var removedFocus = new List<Node>();
        foreach (var node in FocusNodeHelper.GetFocusNodesFromAstRootNode(rootNode))
        {
            var idLeaf = node
                .Leaves.AsValueEnumerable()
                .FirstOrDefault(static leaf => leaf.Key.EqualsIgnoreCase("id"));
            string? id = idLeaf?.ValueText;
            if (id is null)
            {
                continue;
            }

            if (editorNodesMap.TryGetValue(id, out var editorModel))
            {
                // 更新 AST 节点
                SyncNodeContent(node, editorModel);
                editorNodesMap.Remove(id);
            }
            else
            {
                removedFocus.Add(node);
            }
        }

        if (focusTreeNode is not null)
        {
            SyncNode(focusTreeNode, removedFocus, editorNodesMap, FocusType.Normal);
        }
        // 同步 shared_focus
        SyncNode(rootNode, removedFocus, editorNodesMap, FocusType.Shared);

        var fileOrigin = _pathService.GetFileOrigin(filePath);
        if (fileOrigin == FileOrigin.Mod)
        {
            File.WriteAllText(filePath, rootNode.ToScript(), App.Utf8Encoding);
        }
        else if (fileOrigin == FileOrigin.Game)
        {
            string relativePath = Path.GetRelativePath(_settingsService.GameRootFolderPath, filePath);
            string modFilePath = Path.Combine(_settingsService.ModRootFolderPath, relativePath);
            File.WriteAllText(modFilePath, rootNode.ToScript(), App.Utf8Encoding);
        }
        else
        {
            Log.Error("保存文件中遇到无法识别的文件来源: {FilePath}", filePath);
        }
    }

    private static void SyncNode(
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

    private static void SyncNodeContent(Node focusNode, FocusNode editorModel)
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

    [ObservableProperty]
    private double _scale = 1.0;

    [ObservableProperty]
    private double _translateX;

    [ObservableProperty]
    private double _translateY;

    private void LoadTestData()
    {
        var focus = new FocusNode("", FocusType.Normal)
        {
            Id = "GER_Test1",
            RawPosition = new FocusPoint(0, 0),
            Icon = "GFX_goal_test",
        };
        _nodes.Add(new FocusNodeViewModel(focus));
        _nodes.Add(
            new FocusNodeViewModel(
                new FocusNode("", FocusType.Normal)
                {
                    Id = "GER_Test2",
                    RawPosition = new FocusPoint(2, 0),
                    Icon = "GFX_GER_Test2",
                    RelativePosition = focus
                }
            )
        );
        _nodes.Add(
            new FocusNodeViewModel(
                new FocusNode("", FocusType.Normal)
                {
                    Id = "GER_Test3",
                    RawPosition = new FocusPoint(0, 1),
                    Icon = "GFX_GER_Test3",
                }
            )
        );
        _nodes.Add(
            new FocusNodeViewModel(
                new FocusNode("", FocusType.Normal)
                {
                    Id = "GER_Test4",
                    RawPosition = new FocusPoint(2, 1),
                    Icon = "GFX_GER_Test4",
                }
            )
        );
    }
}
