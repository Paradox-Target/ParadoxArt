using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hoi4BlueprintEditor.Services;
using Microsoft.Win32;

namespace Hoi4BlueprintEditor.ViewsModels.Dialogs;

public sealed partial class CreateModViewModel(SettingsService settings) : ObservableObject
{
    [ObservableProperty]
    private string _modName = string.Empty;

    [ObservableProperty]
    private string _rootFolder = string.Empty;

    [ObservableProperty]
    private string _version = string.Empty;

    partial void OnModNameChanged(string value)
    {
        RootFolder = Path.Combine(settings.ModRootFolderPath, value);
    }

    [RelayCommand]
    private void SelectRootFolder()
    {
        Directory.CreateDirectory(RootFolder);

        var openFolderDialog = new OpenFolderDialog
        {
            Title = "选择Mod文件夹",
            Multiselect = false,
            InitialDirectory = settings.ModRootFolderPath,
        };

        if (openFolderDialog.ShowDialog() != true)
        {
            return;
        }

        RootFolder = openFolderDialog.FolderName;
        ModName = Path.GetFileName(RootFolder);
    }
}
