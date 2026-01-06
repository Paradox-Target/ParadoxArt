using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Hoi4BlueprintBuilder.Core.Extensions;
using Hoi4BlueprintBuilder.Core.Helpers;
using Hoi4BlueprintBuilder.Core.Services;
using Hoi4BlueprintBuilder.Core.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NLog;

namespace Hoi4BlueprintBuilder.Core;

public sealed class App : Application
{
    public static readonly Version Version = new(0, 2, 0);
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
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

    /// <summary>
    /// 不带 BOM 的 UTF-8
    /// </summary>
    public static readonly Encoding Utf8Encoding = new UTF8Encoding(false);

    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        NLogSetupHelper.Setup();
        InitializeServices();

        Directory.CreateDirectory(ConfigFolder);
#if DEBUG
        this.AttachDeveloperTools();
#endif

        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            Log.Error(args.Exception, "Task线程中有未被处理的异常");
        };
        Dispatcher.UIThread.UnhandledException += (_, e) =>
        {
            Log.Error(e.Exception, "UI线程中有未被处理的异常");
        };
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
        var settingsService = Services.GetRequiredService<SettingsService>();
        RequestedThemeVariant = settingsService.ThemeMode.ToThemeVariant();

        var telemetryService = Services.GetRequiredService<TelemetryService>();

        telemetryService.TrackEvent("AppStart");

        string? screenSize = null;
        string? screenScaling = null;
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit.
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();
            desktop.MainWindow = Services.GetRequiredService<MainWindow>();
            desktop.Exit += OnDesktopExit;

            if (desktop.MainWindow.Screens.Primary is not null)
            {
                var size = desktop.MainWindow.Screens.Primary.Bounds.Size;
                screenSize = $"{size.Width}x{size.Height}";
                screenScaling = $"{desktop.MainWindow.Screens.Primary.Scaling:P0}";
            }
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = Services.GetRequiredService<MainWindow>();
        }

        Task.Run(() => telemetryService.TrackSystemEnvironment(screenSize, screenScaling));

        base.OnFrameworkInitializationCompleted();
    }

    private void OnDesktopExit(
        object? o,
        ControlledApplicationLifetimeExitEventArgs controlledApplicationLifetimeExitEventArgs
    )
    {
        _stopwatch.Stop();
        var telemetryService = Services.GetRequiredService<TelemetryService>();
        telemetryService.TrackMetric("AppDurationSeconds", _stopwatch.Elapsed.TotalSeconds);
        telemetryService.TrackAppMemoryUsage();

        // TODO: 安卓平台的资源清理
        Services.Dispose();
        LogManager.Flush();
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
