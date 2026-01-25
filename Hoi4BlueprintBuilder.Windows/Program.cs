using Avalonia;
using Hoi4BlueprintBuilder.Core;
using Hoi4BlueprintBuilder.Core.Helpers;
using Hoi4BlueprintBuilder.Core.Services;
using Hoi4BlueprintBuilder.Windows.Services;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using Velopack;

namespace Hoi4BlueprintBuilder.Windows;

public sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        try
        {
            NLogSetupHelper.Setup();
            VelopackApp.Build().Run();

            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception e)
        {
            // 不写成静态字段是因为创建静态字段时未运行初始化日志组件的代码
            var log = LogManager.GetCurrentClassLogger();
            log.Error(e, "程序发生未捕获错误");
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder
            .Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
            .UseR3()
            .AfterSetup(appBuilder =>
            {
                var app = (App?)appBuilder.Instance ?? throw new ArgumentException();
                app.ConfiguringServices += static serviceCollection =>
                {
                    serviceCollection.AddSingleton<IFileSortComparer, WindowsFileSortComparer>();
                };
            });
    }
}
