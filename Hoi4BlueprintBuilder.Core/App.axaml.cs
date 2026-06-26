using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Text;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
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
    public const string AppName = "ParadoxArt";
    public static readonly Version Version =
        Assembly.GetExecutingAssembly().GetName().Version ?? new Version();
    public static new App Current => (App)Application.Current!;

    public ServiceProvider Services { get; private set; } = null!;
    public event EventHandler? OnExitBefore;
    public static readonly string[] AppUpdateChannels = ["stable", "beta"];

    public static string AppFolder { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), AppName);

    public const string ProjectConfigDirectoryName = ".paradoxart";
    public const string UpdatePackageDownloadUrl = "https://packages.paradoxtarget.top";
    public static string ConfigFolder { get; } = Path.Combine(AppFolder, "Config");
    public static string LogsFolder { get; } = GetLogsFolder();

    private static string GetLogsFolder()
    {
        if (OperatingSystem.IsMobile)
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                AppName,
                "Logs"
            );
        }

        return Path.Combine(Environment.CurrentDirectory, "Logs");
    }

    public event Action<IServiceCollection>? ConfiguringServices;

    private ServiceCollection? _serviceCollection;
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

    /// <summary>
    /// 不带 BOM 的 UTF-8
    /// </summary>
    public static readonly Encoding Utf8EncodingWithoutBom = new UTF8Encoding(false);

    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

        InitializeServices();

        Directory.CreateDirectory(ConfigFolder);
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
        _serviceCollection.AddSingleton(static sp =>
        {
            var settings = sp.GetRequiredService<SettingsService>();
            return ProjectConfigService.Load(settings);
        });
        _serviceCollection.AddParadoxArtCore();
        _serviceCollection.AddMessagePipe();
        _serviceCollection.AddSingleton(
            ApplicationLifetime ?? throw new InvalidOperationException("ApplicationLifetime未初始化")
        );
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (_serviceCollection is null)
        {
            throw new InvalidOperationException("serviceCollection未初始化");
        }

        ConfiguringServices?.Invoke(_serviceCollection);

        _serviceCollection.TryAddSingleton<IFileSortComparer, DefaultFileSortComparer>();
        _serviceCollection.TryAddSingleton<IOperatingSystemService, DefaultOperatingSystemService>();

        Services = _serviceCollection.BuildServiceProvider();

        var settingsService = Services.GetRequiredService<SettingsService>();
        RequestedThemeVariant = settingsService.ThemeMode.ToThemeVariant();

        UpdateApplicationFont(settingsService.MainFontFamily);
        UpdateApplicationCodeFont(settingsService.CodeFontFamily);
        var rawCulture = CultureInfo.CurrentUICulture;
        LanguageHelper.SetLanguage(settingsService.AppLanguage);

        var telemetryService = Services.GetRequiredService<TelemetryService>();
        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            Log.Error(args.Exception, "Task线程中有未被处理的异常");
            telemetryService.TrackException(args.Exception, "Task线程中有未被处理的异常");
        };
        Dispatcher.UIThread.UnhandledException += (_, e) =>
        {
            Log.Error(e.Exception, "UI线程中有未被处理的异常");
            telemetryService.TrackException(e.Exception, "UI线程中有未被处理的异常");
        };
        telemetryService.TrackEvent("AppStart");

        string? screenSize = null;
        string? screenScaling = null;
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownRequested += OnOSShutdownRequested;
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

        Task.Run(() => telemetryService.TrackSystemEnvironment(rawCulture, screenSize, screenScaling));

        base.OnFrameworkInitializationCompleted();
    }

    private static void OnOSShutdownRequested(object? sender, ShutdownRequestedEventArgs e)
    {
        //TODO: FIX
        // if (OperatingSystem.IsWindows())
        // {
        //     e.Cancel = true;
        //     var service = Current.Services.GetRequiredService<IOperatingSystemService>();
        //     service.ShutdownBlockReasonCreate("有工作正在进行中...");
        // }
    }

    public void UpdateApplicationFont(string? fontName)
    {
        if (string.IsNullOrWhiteSpace(fontName))
        {
            return;
        }

        try
        {
            Resources["MainFontFamily"] = new FontFamily(fontName);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "无法设置字体: {FontName}", fontName);
        }
    }

    public void UpdateApplicationCodeFont(string? fontName)
    {
        if (string.IsNullOrWhiteSpace(fontName))
        {
            return;
        }

        try
        {
            Resources["CodeFontFamily"] = new FontFamily(fontName);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "无法设置字体: {FontName}", fontName);
        }
    }

    private void OnDesktopExit(
        object? o,
        ControlledApplicationLifetimeExitEventArgs controlledApplicationLifetimeExitEventArgs
    )
    {
        OnExitBefore?.Invoke(o, controlledApplicationLifetimeExitEventArgs);

        _stopwatch.Stop();
        var telemetry = Services.GetRequiredService<TelemetryService>();
        telemetry.TrackMetric("AppDurationSeconds", _stopwatch.Elapsed.TotalSeconds);
        telemetry.TrackAppMemoryUsage();

        // TODO: 安卓平台的资源清理
        Services.Dispose();
        LogManager.Flush();
    }
}
