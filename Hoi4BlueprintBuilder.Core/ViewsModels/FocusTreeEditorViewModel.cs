using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Collections;
using Avalonia.Controls.Notifications;
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
using ParadoxPower.ZLinq;
using ZLinq;

namespace Hoi4BlueprintBuilder.Core.ViewsModels;

[RegisterTransient<FocusTreeEditorViewModel>]
public sealed partial class FocusTreeEditorViewModel : ObservableObject, IClosed
{
    public IAvaloniaList<FocusNodeViewModel> Nodes => _nodes;

    /// <summary>
    /// 可独立切换的叶子条件列表 (不包含 has_completed_focus 类条件, 由 Focus 节点 checkbox 管理)
    /// </summary>
    public IReadOnlyList<ConditionItem> ConditionItems => _conditionItems;

    /// <summary>
    /// 已完成的 Focus ID 集合
    /// </summary>
    public HashSet<string> CompletedFocusIds => _completedFocusIds;

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
    private readonly List<ConditionItem> _conditionItems = [];
    private readonly List<IFocusTrigger> _allTriggers = [];
    private readonly HashSet<string> _completedFocusIds = [];
    private readonly GameResourcesPathService _pathService;
    private readonly SettingsService _settingsService;
    private readonly NotificationService _notificationService;
    private readonly FocusTreeFileService _focusTreeFileService;

    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public FocusTreeEditorViewModel(
        GameResourcesPathService pathService,
        SettingsService settingsService,
        NotificationService notificationService,
        StatusBarService statusBarService,
        FocusTreeFileService focusTreeFileService
    )
    {
        _pathService = pathService;
        _settingsService = settingsService;
        _notificationService = notificationService;
        _focusTreeFileService = focusTreeFileService;

        _nodes.CollectionChanged += (_, _) => statusBarService.SetCurrentFocusCount(_nodes.Count);
    }

    public void OnLoaded()
    {
        StrongReferenceMessenger.Default.Register<CreateNewFocusMessage>(this, CreateNewFocus);
        StrongReferenceMessenger.Default.Register<DeleteImageResourceMessage>(this, DeleteImageResource);
        StrongReferenceMessenger.Default.Register<FocusCompletedChangedMessage>(
            this,
            (_, message) => ToggleFocusCompleted(message.FocusId)
        );
    }

    public void OnUnLoaded()
    {
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
                IEnumerable<string> FilePaths,
                List<ConditionItem> ConditionItems
            )?>(() =>
            {
                if (!TextParser.TryParse(filePath, out var rootNode, out _))
                {
                    return null;
                }
                return _focusTreeFileService.GetAllNodesFromAst(filePath, rootNode);
            });

            ClearResources();

            if (result is null)
            {
                _notificationService.Show("请检查文件格式是否正确", "加载国策树文件失败", NotificationType.Error);
                return;
            }

            var (focusNodes, filePaths, conditionItems) = result.Value;
            _editorNodesMap = focusNodes;
            _focusTreeFiles.AddRange(filePaths);
            _nodes.AddRange(
                _editorNodesMap.Values.Select(static focusNode => new FocusNodeViewModel(focusNode))
            );

            // 收集所有 trigger (offset + allow_branch)
            _allTriggers.AddRange(_editorNodesMap.Values.SelectMany(node => node.Offsets));
            _allTriggers.AddRange(
                _editorNodesMap
                    .Values.Where(node => node.AllowBranch is not null)
                    .Select(node => node.AllowBranch!)
            );

            // 设置条件列表, “has_completed_focus” 类条件由节点 checkbox 管理, 不在条件面板显示
            foreach (var item in conditionItems)
            {
                if (IsCompletedFocusCondition(item))
                {
                    continue;
                }
                _conditionItems.Add(item);
                item.PropertyChanged += OnConditionItemPropertyChanged;
            }

            // 设置哪些 focus 节点显示完成 checkbox
            SetupFocusCompletedCheckboxVisibility(conditionItems);

            StrongReferenceMessenger.Default.Send(RedrawFocusConnectionLinesMessage.Instance);

            Log.Info("已加载国策树文件: {FilePath}", filePath);
            Log.Info(
                "共添加: {Amount}, 来自 {Count} 个文件, Paths: {Paths}",
                _nodes.Count,
                _focusTreeFiles.Count,
                string.Join(", ", _focusTreeFiles.Select(Path.GetFileName))
            );
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

    public bool TryGetFocus(string focusId, [NotNullWhen(true)] out FocusNode? focusNode)
    {
        return _editorNodesMap.TryGetValue(focusId, out focusNode);
    }

    public bool TryGetFocusNodeViewModel(
        string focusId,
        [NotNullWhen(true)] out FocusNodeViewModel? viewModel
    )
    {
        foreach (var node in _nodes)
        {
            if (node.Node.Id == focusId)
            {
                viewModel = node;
                return true;
            }
        }

        viewModel = null;
        return false;
    }

    public void ClearSelection()
    {
        foreach (var node in _nodes)
        {
            node.IsSelected = false;
        }
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

        foreach (var item in _conditionItems)
        {
            item.PropertyChanged -= OnConditionItemPropertyChanged;
        }
        _conditionItems.Clear();
        _allTriggers.Clear();
        _completedFocusIds.Clear();
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
        StrongReferenceMessenger.Default.Send(new SaveFocusTreeMessage());

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

        var focusTreeNode = rootNode.NodesValue.FirstOrDefault(static node =>
            node.Key.EqualsIgnoreCase("focus_tree")
        );

        var removedFocus = new List<Node>();
        foreach (var node in NodeHelper.GetFocusNodesFromAstRootNode(rootNode))
        {
            var idLeaf = node.LeavesValue.FirstOrDefault(static leaf => leaf.Key.EqualsIgnoreCase("id"));
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

    /// <summary>
    ///
    /// </summary>
    /// <param name="source"></param>
    /// <param name="target"></param>
    /// <param name="addType"></param>
    /// <param name="indexOfPrerequisite">添加到指定的前提条件组中</param>
    public void CreateConnection(
        FocusNode source,
        FocusNode target,
        ConnectionType addType,
        int indexOfPrerequisite = -1
    )
    {
        if (ReferenceEquals(source, target))
        {
            return;
        }

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
            if (indexOfPrerequisite != -1)
            {
                source.AddPrerequisite(indexOfPrerequisite, target);
            }
            else
            {
                source.AddPrerequisite([target]);
            }
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
            StrongReferenceMessenger.Default.Send(RedrawFocusConnectionLinesMessage.Instance);
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

        StrongReferenceMessenger.Default.Send(RedrawFocusConnectionLinesMessage.Instance);
    }

    public void Close()
    {
        StrongReferenceMessenger.Default.UnregisterAll(this);
        ClearResources();
    }

    /// <summary>
    /// 当 ConditionItem 的 IsEnabled 变化时, 重新求值所有 trigger
    /// </summary>
    private void OnConditionItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ConditionItem.IsEnabled))
        {
            ReevaluateAllTriggers();
        }
    }

    /// <summary>
    /// 切换 Focus 完成状态
    /// </summary>
    /// <param name="focusId">Focus ID</param>
    private void ToggleFocusCompleted(string focusId)
    {
        if (!_completedFocusIds.Remove(focusId))
        {
            _completedFocusIds.Add(focusId);

            // 取消互斥 focus 的完成状态
            if (_editorNodesMap.TryGetValue(focusId, out var focusNode))
            {
                foreach (var exclusive in focusNode.MutuallyExclusive)
                {
                    _completedFocusIds.Remove(exclusive.Id);
                    // 同步 ViewModel 的 IsCompleted 状态
                    foreach (var vm in _nodes.AsValueEnumerable().Where(vm => vm.Node.Id == exclusive.Id))
                    {
                        vm.IsCompleted = false;
                    }
                }
            }
        }

        ReevaluateAllTriggers();
    }

    /// <summary>
    /// 重新求值所有 trigger 的 IsEnabled 状态
    /// </summary>
    private void ReevaluateAllTriggers()
    {
        // 构建当前为 true 的条件集合
        var trueSet = new HashSet<(string ScopeName, string NodeContent)>();

        // 添加用户勾选的条件
        foreach (var item in _conditionItems)
        {
            if (item.IsEnabled)
            {
                trueSet.Add((item.ScopeName, item.NodeContent));
            }
        }

        // 添加已完成的 focus 条件
        foreach (string focusId in _completedFocusIds)
        {
            trueSet.Add((string.Empty, $"has_completed_focus = {focusId}"));
        }

        // 对每个 trigger 进行求值
        foreach (
            var trigger in _allTriggers.AsValueEnumerable().Where(trigger => trigger.Expression is not null)
        )
        {
            trigger.IsEnabled = ConditionHelper.Evaluate(trigger.Expression!, trueSet);
        }
    }

    /// <summary>
    /// 判断条件项是否为 has_completed_focus 类型
    /// </summary>
    private static bool IsCompletedFocusCondition(ConditionItem item)
    {
        return item is ConditionLeafItem { ScopeName: "" } leafItem
            && leafItem.Leaf.Key.EqualsIgnoreCase("has_completed_focus");
    }

    /// <summary>
    /// 设置哪些 Focus 节点显示完成 CheckBox
    /// </summary>
    private void SetupFocusCompletedCheckboxVisibility(List<ConditionItem> allConditionItems)
    {
        // 收集所有 has_completed_focus 条件引用的 focus ID
        var completedFocusConditionIds = new HashSet<string>();
        foreach (var item in allConditionItems)
        {
            if (
                item is ConditionLeafItem { ScopeName: "" } leafItem
                && leafItem.Leaf.Key.EqualsIgnoreCase("has_completed_focus")
                && !string.IsNullOrEmpty(leafItem.Leaf.ValueText)
            )
            {
                completedFocusConditionIds.Add(leafItem.Leaf.ValueText);
            }
        }

        // 设置对应的 FocusNodeViewModel
        foreach (var vm in _nodes)
        {
            vm.ShowCompletedCheckbox = completedFocusConditionIds.Contains(vm.Node.Id);
        }
    }
}
