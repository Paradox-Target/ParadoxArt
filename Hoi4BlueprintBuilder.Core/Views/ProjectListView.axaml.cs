using Avalonia.Controls;
using Avalonia.Interactivity;
using Hoi4BlueprintBuilder.Core.Models;
using Hoi4BlueprintBuilder.Core.ViewsModels;

namespace Hoi4BlueprintBuilder.Core.Views;

[RegisterTransient<ProjectListView>]
public sealed partial class ProjectListView : UserControl
{
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
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);
        (DataContext as ProjectListViewModel)?.Cleanup();
    }
}
