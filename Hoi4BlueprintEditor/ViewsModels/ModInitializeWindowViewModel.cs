using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hoi4BlueprintEditor.Models;
using Hoi4BlueprintEditor.Resources;
using Hoi4BlueprintEditor.Services;
using Hoi4BlueprintEditor.Views.Dialogs;
using Hoi4BlueprintEditor.ViewsModels.Dialogs;
using iNKORE.UI.WPF.Modern.Controls;
using Microsoft.Win32;
using MessageBox = System.Windows.MessageBox;

namespace Hoi4BlueprintEditor.ViewsModels;

[RegisterTransient<ModInitializeWindowViewModel>]
public sealed partial class ModInitializeWindowViewModel(SettingsService settings) : ObservableObject
{
    public Window? Window { get; set; }

    [RelayCommand]
    private async Task CreateMod()
    {
        var viewModel = new CreateModViewModel(settings)
        {
            ModName = "新建mod",
            Version = DefaultSettings.CurrentGameVersion.ToString(),
        };
        var dialog = new ContentDialog
        {
            Title = "新建 Mod",
            Content = new CreateModView() { DataContext = viewModel },
            CloseButtonText = "取消",
            PrimaryButtonText = "创建",
            DefaultButton = ContentDialogButton.Primary,
            IsPrimaryButtonEnabled = true,
        };
        var result = await dialog.ShowAsync(Window);
        if (result != ContentDialogResult.Primary)
        {
            return;
        }

        try
        {
            var modData = new ModData
            {
                ModName = viewModel.ModName,
                SupportedVersion = Version.Parse(viewModel.Version),
            };
            Directory.CreateDirectory(viewModel.RootFolder);
            string modFilePath = Path.Combine(viewModel.RootFolder, DefaultSettings.ModDescriptorFileName);
            File.WriteAllText(modFilePath, modData.ToScript());

            string focusFolderPath = Path.Combine(viewModel.RootFolder, "common", "national_focus");
            Directory.CreateDirectory(focusFolderPath);

            settings.CurrentModData = modData;
        }
        catch (Exception ex)
        {
            MessageBox.Show(Window!, $"创建Mod文件夹失败：{ex.Message}");
            if (Directory.Exists(viewModel.RootFolder))
            {
                Directory.Delete(viewModel.RootFolder, true);
            }
        }
    }

    [RelayCommand]
    private void OpenMod()
    {
        var openFolderDialog = new OpenFolderDialog
        {
            Title = "选择国策树文件",
            Multiselect = false,
            InitialDirectory = settings.ModRootFolderPath,
        };
        if (openFolderDialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            string descriptorPath = Path.Combine(
                openFolderDialog.FolderName,
                DefaultSettings.ModDescriptorFileName
            );
            var modeData = new ModData();
            modeData.ParseScript(descriptorPath);

            settings.CurrentModData = modeData;
        }
        catch (Exception ex)
        {
            MessageBox.Show(Window!, $"打开mod文件夹失败：{ex.Message}");
        }
    }
}
