using Avalonia;
using Avalonia.Markup.Xaml;
using Hoi4BlueprintBuilder.Core.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Hoi4BlueprintBuilder.UnitTests.Avalonia;

public sealed class App : Application
{
    public static new App Current => (App)Application.Current!;
    public ServiceProvider Services { get; private set; } = null!;
    public event EventHandler? OnExitBefore;
    private const string AppName = "ParadoxArt";

    private static string AppFolder { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), AppName);

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

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

        Directory.CreateDirectory(ConfigFolder);
    }
}
