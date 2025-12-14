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
    [ObservableProperty]
    private string _focusCount = "国策数量: 0";

    [ObservableProperty]
    private string _ramUsage = "内存使用: 0 MB";

    private readonly SettingsService _settingsService;
    private readonly AppLocalizationService _appLocalizationService;
    private readonly StatusBarService _statusBarService;

    public MainControlViewModel(
        SettingsService settingsService,
        AppLocalizationService appLocalizationService,
        StatusBarService statusBarService
    )
    {
        _settingsService = settingsService;
        _appLocalizationService = appLocalizationService;
        _statusBarService = statusBarService;

        _statusBarService.UpdateRamUsage += ramUsage =>
        {
            try
            {
                App.Current.Dispatcher.Invoke(() => RamUsage = ramUsage);
            }
            catch (Exception)
            {
                // ignore
            }
        };
        _statusBarService.UpdateFocusCount += focusCount =>
        {
            App.Current.Dispatcher.Invoke(() => FocusCount = focusCount);
        };
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
            InitialDirectory = Path.Combine(_settingsService.ModRootFolderPath, "common", "national_focus"),
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
#if DEBUG
        App.Current.Services.GetRequiredService<NavigationService>().NavigateTo<MainWelcomeView>();
#endif
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

    [RelayCommand]
    private void SaveFocusTreeToPng()
    {
        WeakReferenceMessenger.Default.Send(new SaveFocusTreeToPngMessage());
    }

    [RelayCommand]
    private void Exit()
    {
        if (
            MessageBox.Show("是否需要保存文件?", "退出确认", MessageBoxButton.YesNo, MessageBoxImage.Question)
            == MessageBoxResult.Yes
        )
        {
            SaveFocusFile();
        }
        Application.Current.Shutdown();
    }
}
