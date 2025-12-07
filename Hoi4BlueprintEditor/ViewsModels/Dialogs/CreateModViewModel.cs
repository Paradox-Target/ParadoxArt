using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;

namespace Hoi4BlueprintEditor.ViewsModels.Dialogs;

public sealed partial class CreateModViewModel : ObservableObject
{
    [ObservableProperty]
    private string _modName = string.Empty;

    [ObservableProperty]
    private string _rootFolder = string.Empty;

    [ObservableProperty]
    private string _version = string.Empty;

    partial void OnModNameChanged(string value)
    {
        string path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        RootFolder = Path.Combine(path, "Paradox Interactive", "Hearts of Iron IV", "mod", value);
    }

    [RelayCommand]
    private void SelectRootFolder()
    {
        var openFolderDialog = new OpenFolderDialog
        {
            FolderName = RootFolder,
            Title = "选择Mod文件夹",
            Multiselect = false,
        };

        if (openFolderDialog.ShowDialog() != true)
        {
            return;
        }

        RootFolder = openFolderDialog.FolderName;
    }
}
