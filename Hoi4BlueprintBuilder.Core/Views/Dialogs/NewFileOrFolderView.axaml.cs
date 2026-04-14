using Avalonia.Controls;
using FluentAvalonia.UI.Controls;
using Hoi4BlueprintBuilder.Core.ViewsModels.Dialogs;

namespace Hoi4BlueprintBuilder.Core.Views.Dialogs;

public sealed partial class NewFileOrFolderView : UserControl
{
    public string NewName => OrFolderViewModel.NewName;
    public bool IsInvalid => OrFolderViewModel.HasErrors;
    private NewFileOrFolderViewModel OrFolderViewModel { get; }

    public NewFileOrFolderView(
        FAContentDialog dialog,
        string targetDirectoryPath,
        string defaultName,
        bool isFile
    )
    {
        InitializeComponent();
        OrFolderViewModel = new NewFileOrFolderViewModel(dialog, targetDirectoryPath, defaultName, isFile);
        DataContext = OrFolderViewModel;
    }
}
