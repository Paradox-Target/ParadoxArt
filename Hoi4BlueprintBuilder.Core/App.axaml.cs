using System.Text;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Hoi4BlueprintBuilder.Core.Services;
using Hoi4BlueprintBuilder.Core.Services.GameResources.Base;
using Hoi4BlueprintBuilder.Core.Services.GameResources.Localization;
using Hoi4BlueprintBuilder.Core.ViewModels;
using Hoi4BlueprintBuilder.Core.Views;
using Microsoft.Extensions.DependencyInjection;

namespace Hoi4BlueprintBuilder.Core;

public sealed partial class App : Application
{
    public new static App Current => (App)Application.Current!;
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

    private static ServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        services.AddSingleton(static _ => SettingsService.LoadSettings());
        // services.AddSingleton(static _ => WindowSettingsService.LoadSettings());
        services.AddSingleton<LocalizationFormatService>();
        services.AddSingleton<LocalizationTextColorsService>();
        services.AddSingleton<GameResourcesPathService>();
        services.AddSingleton<GameModDescriptorService>();
        services.AddSingleton<GameResourcesWatcherService>();

        // services.AddSingleton<MainWindow>();
        // services.AddSingleton<MainWindowViewModel>();

        // services.AddSingleton<EditorCanvasViewModel>();

        services.AddHoi4BlueprintBuilderCore();

        return services.BuildServiceProvider();
    }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit.
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();
            desktop.MainWindow = new MainWindow { DataContext = new MainViewModel() };
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = new MainView { DataContext = new MainViewModel() };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove = BindingPlugins
            .DataValidators.OfType<DataAnnotationsValidationPlugin>()
            .ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}
