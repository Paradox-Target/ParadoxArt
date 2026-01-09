using System.ComponentModel.Design;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;
using DotNet.Globbing;
using FluentAvalonia.UI.Controls;
using Hoi4BlueprintBuilder.Core.Models;
using Hoi4BlueprintBuilder.Core.Services;
using Hoi4BlueprintBuilder.Core.ViewsModels;
using ZLinq;

namespace Hoi4BlueprintBuilder.Core.Views;

[RegisterSingleton<FileTreeView>]
public sealed partial class FileTreeView : UserControl
{
    private readonly TabViewService _tabView;
    private readonly UserStatusService _userStatusService;
    private readonly FAMenuFlyout _contextMenu;
    private TreeViewItem? _lastSelectedTreeViewItem;

    private readonly Thickness _rightSelectedItemThickness = new(0.65);
    private static readonly Glob FocusGlob = Glob.Parse(
        "**/common/national_focus/*.txt",
        new GlobOptions { Evaluation = new EvaluationOptions { CaseInsensitive = false } }
    );

    /// <summary>
    /// 设计器使用
    /// </summary>
    public FileTreeView()
        : this(
            new FileTreeViewModel(new SettingsService(), new DefaultFileSortComparer()),
            new TabViewService(new ServiceContainer()),
            new UserStatusService()
        ) { }

    public FileTreeView(
        FileTreeViewModel viewModel,
        TabViewService tabViewService,
        UserStatusService userStatusService
    )
    {
        InitializeComponent();
        DataContext = viewModel;
        _tabView = tabViewService;
        _userStatusService = userStatusService;
        _contextMenu =
            Resources["ContextMenu"] as FAMenuFlyout ?? throw new InvalidOperationException("文件树上下文菜单实例未找到");

        // 在后台添加是为了拦截 TreeView 控件对鼠标右键响应
        FileTree.AddHandler(PointerPressedEvent, FileTreeView_OnPointerPressed, RoutingStrategies.Tunnel);
        _contextMenu.Closing += (_, _) => ClearTreeViewItemRightSelectEffect();
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

        string extension = Path.GetExtension(item.FullPath);
        if (FocusGlob.IsMatch(item.FullPath))
        {
            _tabView.AddTabFromIoc<FocusTreeEditorView>(item.FullPath);
        }
        else if (TextExtensions.AsValueEnumerable().Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            _tabView.AddTabFromIoc<TextEditorView>(item.FullPath);
        }
        else
        {
            _tabView.AddTabFromIoc<NotSupportInfoControlView>(item.FullPath);
        }
    }

    private static readonly string[] TextExtensions =
    [
        ".txt",
        ".md",
        ".json",
        ".mod",
        ".gui",
        ".gfx",
        ".lua",
        ".yml"
    ];
}
