using Avalonia.Controls;
using Hoi4BlueprintBuilder.Core.ViewsModels;

namespace Hoi4BlueprintBuilder.Core.Views;

[RegisterTransient<ProjectSettingsView>]
public sealed partial class ProjectSettingsView : UserControl
{
    /// <summary>
    /// 设计器使用
    /// </summary>
    public ProjectSettingsView()
    {
        InitializeComponent();
    }

    public ProjectSettingsView(ProjectSettingsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        Unloaded += (_, _) => viewModel.OnUnload();
    }
}
