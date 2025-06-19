using System.Globalization;
using System.IO;
using System.Windows;
using Hoi4BlueprintEditor.Services;
using Hoi4BlueprintEditor.ViewModels;
using Hoi4BlueprintEditor.Views;
using Microsoft.Extensions.DependencyInjection;

namespace Hoi4BlueprintEditor;

public sealed partial class App : Application
{
    public static new App Current => (App)Application.Current;
    public IServiceProvider Services { get; } = ConfigureServices();

    public static string AppFolder { get; } =
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Hoi4BlueprintEditor"
        );

    public static string ConfigFolder { get; } = Path.Combine(AppFolder, "Config");

    private MainWindow _main = null!;

    private static ServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        services.AddSingleton<LocalizationService>();
        services.AddSingleton(_ => SettingsService.LoadSettings());

        services.AddSingleton<MainWindow>();
        services.AddSingleton<MainWindowViewModel>();

        services.AddTransient<EditorCanvasViewModel>();

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
        if (!string.IsNullOrEmpty(settingsService.Language))
        {
            var culture = new CultureInfo(settingsService.Language);
            Thread.CurrentThread.CurrentUICulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
        }

        _main = Services.GetRequiredService<MainWindow>();
        _main.ShowDialog();
    }
}
