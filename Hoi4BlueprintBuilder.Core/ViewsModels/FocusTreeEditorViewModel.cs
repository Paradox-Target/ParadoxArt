using System.Diagnostics;
using Avalonia.Collections;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Hoi4BlueprintBuilder.Core.Extensions;
using Hoi4BlueprintBuilder.Core.Helpers;
using Hoi4BlueprintBuilder.Core.Messages;
using Hoi4BlueprintBuilder.Core.Models;
using Hoi4BlueprintBuilder.Core.Models.Focus;
using Hoi4BlueprintBuilder.Core.Services;
using Hoi4BlueprintBuilder.Core.Views;
using MethodTimer;
using NLog;
using ParadoxPower.CSharpExtensions;
using ParadoxPower.Process;
using ZLinq;

namespace Hoi4BlueprintBuilder.Core.ViewsModels;

[RegisterTransient<FocusTreeEditorViewModel>]
public sealed partial class FocusTreeEditorViewModel : ObservableObject, IClosed
{
    public IAvaloniaList<FocusNodeViewModel> Nodes => _nodes;

    public IReadOnlyCollection<IFocusTrigger> FocusTriggers => _focusTriggers;

    [ObservableProperty]
    private bool _isLoading;

    private readonly AvaloniaList<FocusNodeViewModel> _nodes = [];

    /// <summary>
    /// Key: FocusNode.Id, Value: FocusNode
    /// </summary>
    private Dictionary<string, FocusNode> _editorNodesMap = [];

    /// <summary>
    /// 国策来源文件路径
    /// </summary>
    private readonly List<string> _focusTreeFiles = [];
    private readonly List<IFocusTrigger> _focusTriggers = [];
    private readonly GameResourcesPathService _pathService;
    private readonly SettingsService _settingsService;
    private readonly NotificationService _notificationService;
    private readonly StatusBarService _statusBarService;

    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public FocusTreeEditorViewModel(
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

        // _nodes.CollectionChanged += OnNodeChanged;
    }

    public void OnLoaded()
    {
        WeakReferenceMessenger.Default.Register<CreateNewFocusMessage>(this, CreateNewFocus);
        StrongReferenceMessenger.Default.Register<DeleteImageResourceMessage>(this, DeleteImageResource);
    }

    public void OnUnLoaded()
    {
        WeakReferenceMessenger.Default.UnregisterAll(this);
        StrongReferenceMessenger.Default.UnregisterAll(this);
    }

    private void DeleteImageResource(object sender, DeleteImageResourceMessage message)
    {
        foreach (
            var focus in _nodes.AsValueEnumerable().Where(focus => focus.Node.Icon == message.SpriteName)
        )
        {
            focus.Node.RefreshIcon();
        }
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

    public async Task LoadFocusTreeFileAsync(string filePath)
    {
        IsLoading = true;
        try
        {
            var result = await Task.Run<(
                Dictionary<string, FocusNode> Nodes,
                IEnumerable<string> FilePaths
            )?>(() =>
            {
                if (!TextParser.TryParse(filePath, out var rootNode, out _))
                {
                    return null;
                }
                return FocusNodeHelper.GetAllNodesFromAst(filePath, rootNode);
            });

            ClearResources();

            if (result is null)
            {
                _notificationService.Show("加载国策树文件失败", "请检查文件格式是否正确");
                return;
            }

            var (focusNodes, filePaths) = result.Value;
            _editorNodesMap = focusNodes;
            _focusTreeFiles.AddRange(filePaths);
            _nodes.AddRange(
                _editorNodesMap.Values.Select(static focusNode => new FocusNodeViewModel(focusNode))
            );
            _focusTriggers.AddRange(_editorNodesMap.Values.SelectMany(node => node.Offsets));
            _focusTriggers.AddRange(
                _editorNodesMap
                    .Values.Where(node => node.AllowBranch is not null)
                    .Select(node => node.AllowBranch!)
            );

            Log.Info("已加载国策树文件: {FilePath}", filePath);
            Log.Info("共添加: {Amount}, 来自 {Count} 个文件", _nodes.Count, _focusTreeFiles.Count);
        }
        finally
        {
            IsLoading = false;
        }
    }

    public bool ContainsFocus(string focusId)
    {
        return _editorNodesMap.ContainsKey(focusId);
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
        _focusTriggers.Clear();
    }

    [Time]
    public void SaveFocusTree()
    {
        if (_editorNodesMap.Count == 0)
        {
            Log.Info("没有国策树数据可供保存");
            return;
        }

        // 通知本地化服务保存本地化文本
        StrongReferenceMessenger.Default.Send(this, new SaveFocusTreeMessage());

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
            File.WriteAllText(filePath, rootNode.ToScript(), App.Utf8EncodingWithoutBom);
        }
        else if (fileOrigin == FileOrigin.Game)
        {
            string relativePath = Path.GetRelativePath(_settingsService.GameRootFolderPath, filePath);
            string modFilePath = Path.Combine(_settingsService.ModRootFolderPath, relativePath);
            File.WriteAllText(modFilePath, rootNode.ToScript(), App.Utf8EncodingWithoutBom);
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
            if (current.Node == deletedFocusNode)
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

    public void Close()
    {
        WeakReferenceMessenger.Default.UnregisterAll(this);
        StrongReferenceMessenger.Default.UnregisterAll(this);
        ClearResources();
    }
}
