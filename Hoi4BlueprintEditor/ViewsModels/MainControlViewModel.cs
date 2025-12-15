using System.Globalization;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Hoi4BlueprintEditor.Messages;
using Hoi4BlueprintEditor.Services;
using Hoi4BlueprintEditor.Views;
using Hoi4BlueprintEditor.Views.Dialogs;
using Hoi4BlueprintEditor.Views.Initialization;
using Hoi4BlueprintEditor.ViewsModels.Dialogs;
using iNKORE.UI.WPF.Modern.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using NLog;
using ParadoxPower.CSharpExtensions;
using ParadoxPower.Process;
using MessageBox = iNKORE.UI.WPF.Modern.Controls.MessageBox;

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

    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

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
            catch (Exception e)
            {
                Log.Warn(e, "更新内存使用信息失败");
            }
        };
        _statusBarService.UpdateFocusCount += focusCount =>
        {
            try
            {
                App.Current.Dispatcher.Invoke(() => FocusCount = focusCount);
            }
            catch (Exception e)
            {
                Log.Warn(e, "更新国策数量信息失败");
            }
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

    [RelayCommand]
    private async Task CreateNewFile()
    {
        //TODO: 消除重复代码
        string focusTreeDirectory = Path.Combine(
            _settingsService.ModRootFolderPath,
            "common",
            "national_focus"
        );

        Directory.CreateDirectory(focusTreeDirectory);
        var viewModel = new CreateNewFocusTreeFileViewModel();
        var dialog = new ContentDialog
        {
            Title = "新建国策",
            Content = new CreateNewFocusTreeFileView { DataContext = viewModel },
            CloseButtonText = "取消",
            PrimaryButtonText = "创建",
            DefaultButton = ContentDialogButton.Primary,
            IsPrimaryButtonEnabled = false
        };
        Action<bool> onPrimaryEnableChanged = enable => dialog.IsPrimaryButtonEnabled = enable;
        viewModel.PrimaryEnableChanged += onPrimaryEnableChanged;
        var result = await dialog.ShowAsync(App.Current.MainWindow);
        viewModel.PrimaryEnableChanged -= onPrimaryEnableChanged;
        if (result != ContentDialogResult.Primary)
        {
            return;
        }

        string fileName = viewModel.FileName;
        if (!fileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
        {
            fileName += ".txt";
        }

        string filePath = Path.Combine(focusTreeDirectory, fileName);
        if (File.Exists(filePath))
        {
            MessageBox.Show("文件已存在, 无法创建同名文件.", "创建失败", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        try
        {
            await File.WriteAllTextAsync(
                filePath,
                CreateNewFocusTree(viewModel.Id, viewModel.CountryTag, viewModel.IsDefaultFocusTree),
                App.Utf8Encoding
            );
            WeakReferenceMessenger.Default.Send(new OpenFileMessage(filePath));
        }
        catch (Exception e)
        {
            Log.Error(e, "创建国策树文件失败");
            MessageBox.Show($"创建国策树文件失败: {e.Message}", "创建失败", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private string CreateNewFocusTree(string id, string countryTag, bool isDefaultFocusTree)
    {
        var rootNode = new Node(string.Empty);
        var focusTreeNode = new Node("focus_tree");
        var countryNode = new Node("country");
        if (isDefaultFocusTree)
        {
            countryNode.AllArray = [ChildHelper.Leaf("factor", 1)];
        }
        else
        {
            countryNode.AllArray =
            [
                ChildHelper.Leaf("factor", 0),
                ChildHelper.Node(
                    "modifier",
                    [ChildHelper.Leaf("add", 100), ChildHelper.LeafString("tag", countryTag)]
                )
            ];
        }
        focusTreeNode.AllArray =
        [
            ChildHelper.LeafString("id", id),
            countryNode,
            ChildHelper.Leaf("default", isDefaultFocusTree)
        ];

        rootNode.AllArray = [focusTreeNode];
        return rootNode.ToScript();
    }
}
