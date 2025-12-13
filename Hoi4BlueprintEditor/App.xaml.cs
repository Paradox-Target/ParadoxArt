using System.Diagnostics;
using System.Text;
using System.Windows;
using Hoi4BlueprintEditor.Helpers;
using Hoi4BlueprintEditor.Services;
using Hoi4BlueprintEditor.Services.GameResources.Base;
using Hoi4BlueprintEditor.Services.GameResources.Localization;
using Hoi4BlueprintEditor.Views;
using Hoi4BlueprintEditor.ViewsModels;
using Microsoft.Extensions.DependencyInjection;
using NLog;

namespace Hoi4BlueprintEditor;

public sealed partial class App : Application
{
    public static new App Current => (App)Application.Current;
    public ServiceProvider Services { get; } = ConfigureServices();

    public static string AppFolder { get; } =
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Hoi4BlueprintEditor"
        );

    public static string ConfigFolder { get; } = Path.Combine(AppFolder, "Config");

    /// <summary>
    /// 不带 BOM 的 UTF-8
    /// </summary>
    public static readonly Encoding Utf8Encoding = new UTF8Encoding(false);

    private MainWindow _main = null!;

    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private static ServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        services.AddSingleton<AppLocalizationService>();
        services.AddSingleton(static _ => SettingsService.LoadSettings());
        services.AddSingleton(static _ => WindowSettingsService.LoadSettings());
        services.AddSingleton<LocalizationFormatService>();
        services.AddSingleton<LocalizationTextColorsService>();
        services.AddSingleton<GameResourcesPathService>();
        services.AddSingleton<GameModDescriptorService>();
        services.AddSingleton<GameResourcesWatcherService>();

        services.AddSingleton<MainWindow>();
        services.AddSingleton<MainWindowViewModel>();

        services.AddSingleton<EditorCanvasViewModel>();

        services.AddHoi4BlueprintEditor();

        return services.BuildServiceProvider();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        if (!Directory.Exists(AppFolder))
        {
            Directory.CreateDirectory(AppFolder);
        }
        if (!Directory.Exists(ConfigFolder))
        {
            Directory.CreateDirectory(ConfigFolder);
        }

        var settingsService = Services.GetRequiredService<SettingsService>();
        LanguageHelper.SetLanguage(settingsService.AppLanguage);

        _main = Services.GetRequiredService<MainWindow>();
        Verify();
        _main.ShowDialog();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        base.OnExit(e);

        Current.Services.GetRequiredService<WindowSettingsService>().SaveSettings();
        Services.Dispose();
        LogManager.Flush();
    }

    [Conditional("RELEASE")]
    private void Verify()
    {
        Task.Run(async () =>
        {
            try
            {
                var auth = Services.GetRequiredService<AuthService>();
                if (!await auth.IsActivatedAsync())
                {
                    Current.Dispatcher.Invoke(() =>
                    {
                        var viewModel = new ActivateWindowViewModel();
                        var activateWindow = new ActivateWindowView(viewModel);
                        activateWindow.ShowDialog();
                        if (viewModel.IsActivated)
                        {
                            Log.Info("设备激活成功");
                        }
                        else
                        {
                            Shutdown();
                        }
                    });
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "启动验证失败");
                MessageBox.Show(
                    $"启动失败, 请检查网络连接后重试!\n{e.Message}",
                    "错误",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
                Current.Dispatcher.Invoke(Shutdown);
            }
        });
    }
}
