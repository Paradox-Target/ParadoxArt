using Avalonia.Controls;
using Hoi4BlueprintBuilder.Core.ViewsModels.Dialogs;

namespace Hoi4BlueprintBuilder.Core.Views.Dialogs;

public sealed partial class CreateNewProjectView : UserControl
{
    public CreateNewProjectView()
    {
        InitializeComponent();

        if (Design.IsDesignMode)
        {
            DataContext = new CreateNewProjectViewModel { FolderName = "Test" };
        }
    }
}
