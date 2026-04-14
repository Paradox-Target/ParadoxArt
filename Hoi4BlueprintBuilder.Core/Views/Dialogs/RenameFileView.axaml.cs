using Avalonia.Controls;
using FluentAvalonia.UI.Controls;
using Hoi4BlueprintBuilder.Core.Models;
using Hoi4BlueprintBuilder.Core.ViewsModels.Dialogs;

namespace Hoi4BlueprintBuilder.Core.Views.Dialogs;

public sealed partial class RenameFileView : UserControl
{
    public string NewName => ViewModel.NewName;
    public bool IsInvalid => ViewModel.HasErrors;
    private RenameFileViewModel ViewModel { get; }

    public RenameFileView(FAContentDialog dialog, SystemFileItem fileItem)
    {
        InitializeComponent();
        ViewModel = new RenameFileViewModel(dialog, fileItem);
        DataContext = ViewModel;
    }
}
