using System.Diagnostics.CodeAnalysis;
using System.Text;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Hoi4BlueprintBuilder.Core.Helpers;
using Hoi4BlueprintBuilder.Core.Services;
using Hoi4BlueprintBuilder.Core.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NLog;

namespace Hoi4BlueprintBuilder.Core;

public sealed class App : Application
{
    public static new App Current => (App)Application.Current!;
    public Task<bool>? IsActivated { get; private set; }
    public ServiceProvider Services { get; private set; } = null!;

    public static string AppFolder { get; } =
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Hoi4BlueprintEditor"
        );
    public static string ConfigFolder { get; } = Path.Combine(AppFolder, "Config");
    public event Action<IServiceCollection>? ConfiguringServices;

    private ServiceCollection? _serviceCollection;

    /// <summary>
    /// 不带 BOM 的 UTF-8
    /// </summary>
    public static readonly Encoding Utf8Encoding = new UTF8Encoding(false);

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        NLogSetupHelper.Setup();
        InitializeServices();
#if DEBUG
        this.AttachDeveloperTools();
#endif
    }

    [MemberNotNull(nameof(_serviceCollection))]
    private void InitializeServices()
    {
        _serviceCollection = [];

        _serviceCollection.AddSingleton(static _ => SettingsService.LoadSettings());
        _serviceCollection.AddSingleton(static _ => WindowSettingsService.LoadSettings());
        _serviceCollection.AddHoi4BlueprintBuilderCore();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (_serviceCollection is null)
        {
            throw new InvalidOperationException();
        }

        ConfiguringServices?.Invoke(_serviceCollection);
        _serviceCollection.TryAddSingleton<IFileSortComparer, DefaultFileSortComparer>();

        Services =
            _serviceCollection?.BuildServiceProvider()
            ?? throw new ArgumentException("serviceCollection未初始化");
        IsActivated = Services.GetRequiredService<AuthService>().IsActivatedAsync();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit.
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();
            desktop.MainWindow = Services.GetRequiredService<MainWindow>();
            desktop.Exit += (_, _) =>
            {
                // TODO: 安卓平台的资源清理
                Services.Dispose();
                LogManager.Flush();
            };
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = Services.GetRequiredService<MainWindow>();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void DisableAvaloniaDataAnnotationValidation()
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
