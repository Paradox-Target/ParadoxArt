using Avalonia;
using Hoi4BlueprintBuilder.Core;
using Hoi4BlueprintBuilder.Core.Services;
using Hoi4BlueprintBuilder.Windows.WindowsServices;
using Microsoft.Extensions.DependencyInjection;

namespace Hoi4BlueprintBuilder.Windows;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>().UsePlatformDetect().WithInterFont().LogToTrace().AfterSetup(appBuilder =>
        {
            var app = (App?)appBuilder.Instance ?? throw new ArgumentException();
            app.ConfiguringServices += static serviceCollection =>
            {
                serviceCollection.AddSingleton<IFileSortComparer, WindowsFileSortComparer>();
            };
        });
}
