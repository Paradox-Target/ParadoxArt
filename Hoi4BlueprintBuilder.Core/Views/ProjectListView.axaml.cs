using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using FluentAvalonia.UI.Controls;
using Hoi4BlueprintBuilder.Core.Models;
using Hoi4BlueprintBuilder.Core.ViewsModels;

namespace Hoi4BlueprintBuilder.Core.Views;

[RegisterTransient<ProjectListView>]
public sealed partial class ProjectListView : UserControl
{
    private readonly FAMenuFlyout? _contextMenu;

    /// <summary>
    /// 设计器使用
    /// </summary>
    public ProjectListView()
    {
        InitializeComponent();

        var list = new List<ProjectItem>();
        for (int i = 16 - 1; i >= 0; i--)
        {
            list.Add(new ProjectItem($"Item{i}", Path.GetTempPath() + $"Item{i}"));
        }
        ProjectListBox.ItemsSource = list;
    }

    public ProjectListView(ProjectListViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        _contextMenu =
            Resources["ProjectItemContextMenu"] as FAMenuFlyout
            ?? throw new InvalidOperationException("项目列表上下文菜单实例未找到");
        ProjectListBox.AddHandler(PointerPressedEvent, OnProjectListPointerPressed, RoutingStrategies.Bubble);
    }

    private void OnProjectListPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.Properties.PointerUpdateKind != PointerUpdateKind.RightButtonPressed)
        {
            return;
        }

        if (e.Source is not InputElement { DataContext: ProjectItem item })
        {
            return;
        }

        if (DataContext is ProjectListViewModel viewModel)
        {
            viewModel.RightClickedItem = item;
        }

        // 以 ProjectListBox（DataContext = ViewModel）为 placement target，使菜单命令可绑定到 ViewModel
        _contextMenu?.ShowAt(ProjectListBox, true);
        e.Handled = true;
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);
        (DataContext as ProjectListViewModel)?.Cleanup();
    }
}
