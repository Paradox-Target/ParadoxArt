using System.Globalization;
using System.IO;
using System.Windows;
using Hoi4BlueprintEditor.Core;
using Hoi4BlueprintEditor.ViewModels;
using Hoi4BlueprintEditor.Views;
using Microsoft.Extensions.DependencyInjection;

namespace Hoi4BlueprintEditor;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public static new App Current => (App)Application.Current;
    public IServiceProvider Services { get; } = ConfigureServices();

    public static string ConfigFolder { get; } =
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Hoi4BlueprintEditor"
        );

    private MainWindow _main = null!;

    private static IServiceProvider ConfigureServices()
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
