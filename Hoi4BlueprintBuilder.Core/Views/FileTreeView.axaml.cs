using System.ComponentModel.Design;
using Avalonia.Controls;
using DotNet.Globbing;
using Hoi4BlueprintBuilder.Core.Models;
using Hoi4BlueprintBuilder.Core.Services;
using Hoi4BlueprintBuilder.Core.ViewsModels;

namespace Hoi4BlueprintBuilder.Core.Views;

[RegisterSingleton<FileTreeView>]
public sealed partial class FileTreeView : UserControl
{
    private readonly TabViewService _tabView;
    private readonly UserStatusService _userStatusService;
    private static readonly Glob FocusGlob = Glob.Parse(
        "**/common/national_focus/*.txt",
        new GlobOptions { Evaluation = new EvaluationOptions { CaseInsensitive = false } }
    );

    /// <summary>
    /// 设计器使用
    /// </summary>
    public FileTreeView()
    {
        InitializeComponent();
        _tabView = new TabViewService(new ServiceContainer());
        _userStatusService = new UserStatusService();
    }

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

        if (!FocusGlob.IsMatch(item.FullPath))
        {
            _tabView.AddTabFromIoc<NotSupportInfoControlView>(item.FullPath);
            return;
        }
        _tabView.AddTabFromIoc<EditorCanvasView>(item.FullPath);
    }
}
