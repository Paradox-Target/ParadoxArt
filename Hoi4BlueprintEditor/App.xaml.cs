using System.Configuration;
using System.Data;
using System.Globalization;
using System.Windows;
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

    private MainWindow _main = null!;

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        services.AddSingleton<Core.ILocalizationService, Core.LocalizationService>();
        services.AddSingleton<Core.ISettingsService, Core.SettingsService>();

        services.AddSingleton<MainWindow>();
        services.AddSingleton(provider =>
        {
            var settings = provider.GetRequiredService<Core.ISettingsService>();
            var localizationService = provider.GetRequiredService<Core.ILocalizationService>();
            return new MainWindowViewModel(settings, localizationService);
        });

        return services.BuildServiceProvider();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var settingsService = Services.GetRequiredService<Core.ISettingsService>();
        settingsService.LoadSettings();
        if (!string.IsNullOrEmpty(settingsService.CurrentSettings.Language))
        {
            var culture = new CultureInfo(settingsService.CurrentSettings.Language);
            Thread.CurrentThread.CurrentUICulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
        }

        _main = Services.GetRequiredService<MainWindow>();
        _main.ShowDialog();
    }
}
