using System.ComponentModel.Design;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;
using FluentAvalonia.UI.Controls;
using Hoi4BlueprintBuilder.Core.Helpers;
using Hoi4BlueprintBuilder.Core.Models;
using Hoi4BlueprintBuilder.Core.Services;
using Hoi4BlueprintBuilder.Core.ViewsModels;

namespace Hoi4BlueprintBuilder.Core.Views;

[RegisterSingleton<FileTreeView>]
public sealed partial class FileTreeView : UserControl
{
    private readonly TabViewService _tabView;
    private readonly UserStatusService _userStatusService;
    private readonly ProjectConfigService _projectConfigService;
    private readonly ProjectPathService _projectPathService;
    private readonly FAMenuFlyout _contextMenu;
    private readonly FileTreeViewModel _viewModel;
    private TreeViewItem? _lastSelectedTreeViewItem;

    private readonly Thickness _rightSelectedItemThickness = new(0.65);

    /// <summary>
    /// 设计器使用
    /// </summary>
    public FileTreeView()
        : this(
            new FileTreeViewModel(SettingsService.LoadSettings(), new DefaultFileSortComparer()),
            new TabViewService(new ServiceContainer()),
            new UserStatusService(),
            ProjectConfigService.Load(SettingsService.LoadSettings()),
            new ProjectPathService(SettingsService.LoadSettings())
        ) { }

    public FileTreeView(
        FileTreeViewModel viewModel,
        TabViewService tabViewService,
        UserStatusService userStatusService,
        ProjectConfigService projectConfigService,
        ProjectPathService projectPathService
    )
    {
        InitializeComponent();
        DataContext = viewModel;
        _viewModel = viewModel;
        _tabView = tabViewService;
        _userStatusService = userStatusService;
        _projectConfigService = projectConfigService;
        _projectPathService = projectPathService;
        _contextMenu =
            Resources["ContextMenu"] as FAMenuFlyout ?? throw new InvalidOperationException("文件树上下文菜单实例未找到");

        // 在后台添加是为了拦截 TreeView 控件对鼠标右键响应
        FileTree.AddHandler(PointerPressedEvent, FileTreeView_OnPointerPressed, RoutingStrategies.Tunnel);
        _contextMenu.Closing += (_, _) => ClearTreeViewItemRightSelectEffect();

        App.Current.OnExitBefore += OnExitBefore;
        Loaded += OnLoaded;

        // 监听 TabView 的当前项变化事件，在关闭所有标签页后清理选中状态
        _tabView.CurrentItemChanged += item =>
        {
            if (item is null)
            {
                FileTree.SelectedItem = null;
            }
        };
    }

    private void OnExitBefore(object? o, EventArgs eventArgs)
    {
        _projectConfigService.ExpandedFolders.Clear();
        SaveExpandedFolders(_viewModel.Items);
    }

    private void SaveExpandedFolders(IEnumerable<SystemFileItem> children)
    {
        foreach (var child in children)
        {
            if (child.IsExpanded)
            {
                _projectConfigService.ExpandedFolders.Add(
                    _projectPathService.GetRelativeModPath(child.FullPath)
                );
                SaveExpandedFolders(child.Children);
            }
        }
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        // 恢复上次打开的文件夹的展开状态
        ExpandFolders(_viewModel.Items);
        _projectConfigService.ExpandedFolders.Clear();

        // 恢复上次打开的文件
        var files = FlattenSystemFileItem(_viewModel.Items);
        foreach (string filePath in _projectConfigService.OpenedFiles)
        {
            string fullFilePath = _projectPathService.GetAbsoluteModPath(filePath);

            if (files.Contains(fullFilePath))
            {
                _userStatusService.CurrentSelectedFile = SystemFileItem.FromFilePath(fullFilePath);
                AddNewTabViewByFilePath(fullFilePath);
            }
        }
        _projectConfigService.OpenedFiles.Clear();
    }

    private static HashSet<string> FlattenSystemFileItem(IEnumerable<SystemFileItem> items)
    {
        var files = new HashSet<string>(64);
        FlattenSystemFileItemCore(files, items);
        return files;
    }

    private static void FlattenSystemFileItemCore(HashSet<string> files, IEnumerable<SystemFileItem> items)
    {
        foreach (var item in items)
        {
            if (item.IsFile)
            {
                files.Add(item.FullPath);
            }
            else
            {
                FlattenSystemFileItemCore(files, item.Children);
            }
        }
    }

    private void ExpandFolders(IEnumerable<SystemFileItem> items)
    {
        foreach (var item in items)
        {
            if (
                item.IsFolder
                && _projectConfigService.ExpandedFolders.Contains(
                    _projectPathService.GetRelativeModPath(item.FullPath)
                )
            )
            {
                item.IsExpanded = true;
                ExpandFolders(item.Children);
            }
        }
    }

    private void FileTreeView_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.Pointer.Type != PointerType.Mouse)
        {
            return;
        }

        // 无论左键右键都清理上次右键选中的项的选中效果
        ClearTreeViewItemRightSelectEffect();

        var point = e.GetCurrentPoint(FileTree);

        if (point.Properties.PointerUpdateKind == PointerUpdateKind.RightButtonPressed)
        {
            e.Handled = true;

            var treeViewItem = ((Control?)e.Source)
                ?.GetVisualAncestors()
                .OfType<TreeViewItem>()
                .FirstOrDefault();
            if (treeViewItem is null)
            {
                return;
            }

            _contextMenu.ShowAt(treeViewItem, true);
            _lastSelectedTreeViewItem = treeViewItem;
            treeViewItem.BorderThickness = _rightSelectedItemThickness;
            treeViewItem.BorderBrush = Brushes.CornflowerBlue;
        }
    }

    private void ClearTreeViewItemRightSelectEffect()
    {
        _lastSelectedTreeViewItem?.BorderBrush = Brushes.Transparent;
    }

    private void TreeView_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count == 0)
        {
            return;
        }

        if (e.AddedItems[0] is not SystemFileItem item)
        {
            return;
        }

        if (item.IsFolder)
        {
            return;
        }

        _userStatusService.CurrentSelectedFile = item;

        AddNewTabViewByFilePath(item.FullPath);
    }

    private void AddNewTabViewByFilePath(string filePath)
    {
        if (FileCheckHelper.IsFocusTreeFile(filePath))
        {
            _tabView.AddTabFromIoc<FocusTreeEditorView>(filePath);
        }
        else if (FileCheckHelper.IsTextFile(filePath))
        {
            _tabView.AddTabFromIoc<TextEditorView>(filePath);
        }
        else
        {
            _tabView.AddTabFromIoc<NotSupportInfoControlView>(filePath);
        }
    }
}
