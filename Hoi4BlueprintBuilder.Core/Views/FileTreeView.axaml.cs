using Avalonia.Controls;
using Hoi4BlueprintBuilder.Core.ViewsModels;

namespace Hoi4BlueprintBuilder.Core.Views;

[RegisterSingleton<FileTreeView>]
public sealed partial class FileTreeView : UserControl
{
    /// <summary>
    /// 设计器使用
    /// </summary>
    public FileTreeView()
    {
        InitializeComponent();
    }

    public FileTreeView(FileTreeViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
