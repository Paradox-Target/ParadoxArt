using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hoi4BlueprintEditor.Constants;
using Hoi4BlueprintEditor.Models;
using Hoi4BlueprintEditor.Services;
using Hoi4BlueprintEditor.Views.Dialogs;
using Hoi4BlueprintEditor.ViewsModels.Dialogs;
using iNKORE.UI.WPF.Modern.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using NLog;
using MessageBox = iNKORE.UI.WPF.Modern.Controls.MessageBox;

namespace Hoi4BlueprintEditor.ViewsModels;

[RegisterTransient<ModInitializeWindowViewModel>]
public sealed partial class ModInitializeWindowViewModel(SettingsService settings) : ObservableObject
{
    public Window? Window { get; set; }

    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    [RelayCommand]
    private async Task CreateMod()
    {
        var viewModel = new CreateModViewModel(settings) { ModName = "新建mod", Version = "1.0.0", };
        var dialog = new ContentDialog
        {
            Title = "新建 Mod",
            Content = new CreateModView { DataContext = viewModel },
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
            var modData = new ModData { ModName = viewModel.ModName, SupportedVersion = viewModel.Version, };
            Directory.CreateDirectory(viewModel.RootFolder);
            string modFilePath = Path.Combine(viewModel.RootFolder, GameConstants.ModDescriptorFileName);
            await File.WriteAllTextAsync(modFilePath, modData.ToScript()).ConfigureAwait(false);

            string focusFolderPath = Path.Combine(viewModel.RootFolder, "common", "national_focus");
            Directory.CreateDirectory(focusFolderPath);
        }
        catch (Exception ex)
        {
            MessageBox.Show(Window!, $"创建Mod文件夹失败：{ex.Message}");
            if (Directory.Exists(viewModel.RootFolder))
            {
                Directory.Delete(viewModel.RootFolder, true);
            }
            Log.Error(ex, "创建Mod文件夹失败");
        }
    }

    [RelayCommand]
    private void OpenMod()
    {
        var openFolderDialog = new OpenFolderDialog { Title = "选择 MOD 根目录", Multiselect = false };
        if (openFolderDialog.ShowDialog() != true)
        {
            return;
        }

        settings.ModRootFolderPath = openFolderDialog.FolderName;
        App.Current.Services.GetRequiredService<GameModDescriptorService>().Reload();
        Log.Info("已选择 Mod 根目录: {ModRootFolderPath}", settings.ModRootFolderPath);
    }
}
