using System.Text;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hoi4BlueprintEditor.Services;
using Hoi4BlueprintEditor.Views.Dialogs;
using Hoi4BlueprintEditor.ViewsModels.Dialogs;
using iNKORE.UI.WPF.Modern.Controls;
using Microsoft.Win32;
using ParadoxPower.CSharpExtensions;
using ParadoxPower.Process;
using MessageBox = System.Windows.MessageBox;

namespace Hoi4BlueprintEditor.ViewsModels;

[RegisterTransient<ModInitializeWindowViewModel>]
public sealed partial class ModInitializeWindowViewModel(SettingsService settings) : ObservableObject
{
    public Window? Window { get; set; }

    private void CreateMod(CreateModViewModel viewModel)
    {
        try
        {
            _ = Directory.CreateDirectory(viewModel.RootFolder);
            
            var node = Node.Create(string.Empty);
            node.AddLeafString("version", viewModel.Version);
            node.AddLeafString("name", viewModel.ModName);
            node.AddLeafString("supported_version", viewModel.Version);
            var modFilePath = Path.Combine(viewModel.RootFolder, "descriptor.mod");
            File.WriteAllText(modFilePath, node.ToScript());
            
            var focusFolderPath = Path.Combine(viewModel.RootFolder, "common", "national_focus");
            _ = Directory.CreateDirectory(focusFolderPath);
            
            settings.ModRootFolderPath = viewModel.RootFolder;
        }
        catch (Exception ex)
        {
            MessageBox.Show(Window!, $"创建Mod文件夹失败：{ex.Message}");
        }
    }

    [RelayCommand]
    private async Task CreateMod()
    {
        var viewModel = new CreateModViewModel() { ModName = "新建mod", Version = "1.17.2" };
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

        if (result == ContentDialogResult.Primary)
        {
            CreateMod(viewModel);
        }
    }

    [RelayCommand]
    private void OpenMod()
    {
        var openFolderDialog = new OpenFolderDialog() { Title = "选择国策树文件", Multiselect = false };

        if (openFolderDialog.ShowDialog() != true)
        {
            return;
        }
    }
}
