using Avalonia.Controls;
using Hoi4BlueprintBuilder.Core.Services;
using Hoi4BlueprintBuilder.Core.ViewsModels.Dialogs;

namespace Hoi4BlueprintBuilder.Core.Views.Dialogs;

public sealed partial class CreateNewProjectView : UserControl
{
    public CreateNewProjectViewModel ViewModel => (CreateNewProjectViewModel)DataContext!;

    public CreateNewProjectView()
    {
        InitializeComponent();

        if (Design.IsDesignMode)
        {
            DataContext = new CreateNewProjectViewModel(new SettingsService(), _ => { })
            {
                FolderName = "Test"
            };
        }
    }
}
