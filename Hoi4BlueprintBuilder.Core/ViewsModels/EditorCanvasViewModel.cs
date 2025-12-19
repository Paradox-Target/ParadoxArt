using System.Diagnostics;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Hoi4BlueprintBuilder.Core.Extensions;
using Hoi4BlueprintBuilder.Core.Helpers;
using Hoi4BlueprintBuilder.Core.Messages;
using Hoi4BlueprintBuilder.Core.Models;
using Hoi4BlueprintBuilder.Core.Models.Focus;
using Hoi4BlueprintBuilder.Core.Services;
using MethodTimer;
using NLog;
using ObservableCollections;
using ParadoxPower.CSharpExtensions;
using ParadoxPower.Process;
using ZLinq;

namespace Hoi4BlueprintBuilder.Core.ViewsModels;

public sealed partial class EditorCanvasViewModel : ObservableObject
{
    public NotifyCollectionChangedSynchronizedViewList<FocusNodeViewModel> Nodes { get; }

    private readonly ObservableList<FocusNodeViewModel> _nodes = [];

    /// <summary>
    /// Key: FocusNode.Id, Value: FocusNode
    /// </summary>
    private Dictionary<string, FocusNode> _editorNodesMap = [];

    /// <summary>
    /// 国策来源文件路径
    /// </summary>
    private readonly List<string> _focusTreeFiles = [];
    private readonly GameResourcesPathService _pathService;
    private readonly SettingsService _settingsService;
    private readonly NotificationService _notificationService;
    private readonly StatusBarService _statusBarService;

    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public EditorCanvasViewModel(
        GameResourcesPathService pathService,
        SettingsService settingsService,
        NotificationService notificationService,
        StatusBarService statusBarService
    )
    {
        _pathService = pathService;
        _settingsService = settingsService;
        _notificationService = notificationService;
        _statusBarService = statusBarService;
        Nodes = _nodes.ToNotifyCollectionChanged();
        _nodes.CollectionChanged += OnNodeChanged;
        // 假数据测试
        LoadTestData();

        WeakReferenceMessenger.Default.Register<OpenFileMessage>(this, OnOpenFile);
        WeakReferenceMessenger.Default.Register<SaveFocusTreeMessage>(this, SaveFocusTree);
        WeakReferenceMessenger.Default.Register<CreateNewFocusMessage>(this, CreateNewFocus);
        // 如果某一天EditorCanvasViewModel不是单例模式了, 就需要改一下这个
        StrongReferenceMessenger.Default.Register<DeleteImageResourceMessage>(
            this,
            (_, message) =>
            {
                foreach (
                    var focus in _nodes
                        .AsValueEnumerable()
                        .Where(focus => focus.Model.Icon == message.SpriteName)
                )
                {
                    focus.Model.RefreshIcon();
                }
            }
        );
    }

    private void OnNodeChanged(in NotifyCollectionChangedEventArgs<FocusNodeViewModel> e)
    {
        _statusBarService.SetCurrentFocusCount(_nodes.Count);
    }

    private void CreateNewFocus(object sender, CreateNewFocusMessage message)
    {
        message.Reply(
            Task.Run(() =>
            {
                var focus = new FocusNode(message.FocusFilePath, message.FocusType)
                {
                    RawPosition = message.Position,
                    Id = message.FocusId
                };
                Dispatcher.UIThread.Post(() =>
                {
                    _nodes.Add(new FocusNodeViewModel(focus));
                });
                _editorNodesMap[focus.Id] = focus;
                return focus;
            })
        );
    }

    private void OnOpenFile(object sender, OpenFileMessage message)
    {
        if (!TextParser.TryParse(message.FilePath, out var rootNode, out var _))
        {
            return;
        }

        ClearResources();

        var (focusNodes, filePaths) = FocusNodeHelper.GetAllNodesFromAst(message.FilePath, rootNode);
        _editorNodesMap = focusNodes;
        _focusTreeFiles.AddRange(filePaths);
        _nodes.AddRange(_editorNodesMap.Values.Select(static focusNode => new FocusNodeViewModel(focusNode)));
        Log.Info("已加载国策树文件: {FilePath}", message.FilePath);
        Log.Info("共添加: {Amount}, 来自 {Count} 个文件", _nodes.Count, _focusTreeFiles.Count);
    }

    // 从 2 开始, 但先检查 1 是否被使用
    private static uint _focusId = 2;

    /// <summary>
    /// 获取下一个可用的国策 Id
    /// </summary>
    /// <remarks>线程不安全</remarks>
    /// <returns></returns>
    public string GetNextFocusId()
    {
        // 有可能 Id返回后并没有真的被使用，所以先减一, 检查是否真的被使用
        string id = $"new_focus_{_focusId - 1}";
        if (!_editorNodesMap.ContainsKey(id))
        {
            return id;
        }

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
        _editorNodesMap.Clear();
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
                NodeHelper.SyncNodeContent(node, editorModel);
                editorNodesMap.Remove(id);
            }
            else
            {
                removedFocus.Add(node);
            }
        }

        if (focusTreeNode is not null)
        {
            NodeHelper.SyncNodeChildren(focusTreeNode, removedFocus, editorNodesMap, FocusType.Normal);
        }
        // 同步 shared_focus
        NodeHelper.SyncNodeChildren(rootNode, removedFocus, editorNodesMap, FocusType.Shared);

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
                    RelativePosition = focus,
                }
            )
        );
        var f3 = new FocusNode("", FocusType.Normal)
        {
            Id = "GER_Test3",
            RawPosition = new FocusPoint(0, 1),
            Icon = "GFX_GER_Test3",
        };
        f3.AddPrerequisite([focus]);
        _nodes.Add(new FocusNodeViewModel(f3));
        var f4 = new FocusNode("", FocusType.Normal)
        {
            Id = "GER_Test4",
            RawPosition = new FocusPoint(2, 1),
            Icon = "GFX_GER_Test4",
        };
        f4.AddPrerequisite([focus]);
        _nodes.Add(new FocusNodeViewModel(f4));

        _focusTreeFiles.Add("TestFocusFile1.txt");
        _focusTreeFiles.Add("TestFocusFile2.txt");
    }

    public IReadOnlyCollection<string> GetAllFocusFiles()
    {
        return _focusTreeFiles;
    }

    public void CreateConnection(FocusNode source, FocusNode target, ConnectionType addType)
    {
        bool changed = false;

        if (
            addType == ConnectionType.MutuallyExclusive
            && !source.MutuallyExclusive.Contains(target)
            && !target.Children.Contains(source)
            && !source.Children.Contains(target)
        )
        {
            source.AddMutuallyExclusive(target);
            changed = true;
        }
        else if (
            addType == ConnectionType.Prerequisite
            // 检查是否已经存在于任何前置组中
            && !target.Children.Contains(source)
            // 互斥的时候不能作为前置条件
            && !source.MutuallyExclusive.Contains(target)
        )
        {
            source.AddPrerequisite([target]);
            changed = true;
        }
        else if (addType == ConnectionType.RelativePosition)
        {
            bool isSuccessful = source.ConvertToRelativePosition(target);
            if (!isSuccessful)
            {
                _notificationService.Show("无法建立相对位置连接, 因为会导致循环引用");
            }
            changed = isSuccessful;
        }

        if (changed)
        {
            WeakReferenceMessenger.Default.Send(RedrawFocusConnectionLinesMessage.Instance);
        }
    }

    public void DeleteFocusNode(FocusNode deletedFocusNode)
    {
        if (!_editorNodesMap.Remove(deletedFocusNode.Id))
        {
            Log.Warn("删除Focus失败, 未在映射表中找到对应的 FocusNode: {FocusId}", deletedFocusNode.Id);
            return;
        }

        FocusNodeViewModel? viewModel = null;
        int index = 0;
        for (; index < _nodes.Count; index++)
        {
            var current = _nodes[index];
            if (current.Model == deletedFocusNode)
            {
                viewModel = current;
                break;
            }
        }
        if (viewModel is not null)
        {
            _nodes.RemoveAt(index);
            viewModel.Dispose();
        }
        else
        {
            Log.Warn("删除Focus失败, 未找到对应的 FocusNodeViewModel: {FocusId}", deletedFocusNode.Id);
            return;
        }

        WeakReferenceMessenger.Default.Send(RedrawFocusConnectionLinesMessage.Instance);
    }
}
