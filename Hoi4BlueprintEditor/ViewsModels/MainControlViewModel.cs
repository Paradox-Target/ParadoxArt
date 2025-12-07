using System.Globalization;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Hoi4BlueprintEditor.Messages;
using Hoi4BlueprintEditor.Services;
using Hoi4BlueprintEditor.Views;
using Hoi4BlueprintEditor.Views.Initialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;

namespace Hoi4BlueprintEditor.ViewsModels;

[RegisterSingleton<MainControlViewModel>]
public sealed partial class MainControlViewModel : ObservableObject
{
    private readonly SettingsService _settingsService;
    private readonly AppLocalizationService _appLocalizationService;

    public MainControlViewModel(
        SettingsService settingsService,
        AppLocalizationService appLocalizationService
    )
    {
        _settingsService = settingsService;
        _appLocalizationService = appLocalizationService;
    }

    private void SetLanguage(string cultureCode)
    {
        var targetCulture = new CultureInfo(cultureCode);

        string message = _appLocalizationService.GetString("LanguageRestartMessage", targetCulture);
        string title = _appLocalizationService.GetString("RestartRequiredTitle", targetCulture);

        _settingsService.AppLanguage = cultureCode;
        _settingsService.SaveSettings();

        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
    }

    [RelayCommand]
    private void SetLanguageToEnglish() => SetLanguage("en-US");

    [RelayCommand]
    private void SetLanguageToChinese() => SetLanguage("zh-CN");

    [RelayCommand]
    private void OpenFocusFile()
    {
        var openFileDialog = new OpenFileDialog
        {
            Filter = "Focus Files (*.txt)|*.txt|All Files (*.*)|*.*",
            Title = "选择国策树文件",
            Multiselect = false,
        };

        if (openFileDialog.ShowDialog() != true)
        {
            return;
        }

        string filePath = openFileDialog.FileName;
        WeakReferenceMessenger.Default.Send(new OpenFileMessage(filePath));
    }

    [RelayCommand]
    private void SaveFocusFile()
    {
        WeakReferenceMessenger.Default.Send(new SaveFocusTreeMessage(""));
    }

    [RelayCommand]
    private void GameSettings()
    {
        // TODO: 暂时仅供界面测试
        //  如果已编辑提示是否保存
        //  使用弹窗而不是欢迎界面
        App.Current.Services.GetRequiredService<NavigationService>().NavigateTo<MainWelcomeView>();
    }

    [RelayCommand]
    private void OpenSettingsView()
    {
        var view = App.Current.Services.GetRequiredService<SettingsWindowView>();
        view.ShowDialog();
    }

    [RelayCommand]
    private void OpenModInitializePageView()
    {
        // TODO: 仅供测试，应该把新建和读取分成两个菜单项
        var view = App.Current.Services.GetRequiredService<ModInitializeWindowView>();
        view.ShowDialog();
    }
}
