using Android.Runtime;
using Avalonia;
using Avalonia.Android;
using Hoi4BlueprintBuilder.Androi.Services;
using Hoi4BlueprintBuilder.Core;
using Hoi4BlueprintBuilder.Core.Helpers;
using Hoi4BlueprintBuilder.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Hoi4BlueprintBuilder.Androi;

[Application]
// ReSharper disable once UnusedType.Global
// ReSharper disable once ClassCannotBeInstantiated
public sealed class Application : AvaloniaAndroidApplication<App>
{
    protected Application(nint javaReference, JniHandleOwnership transfer)
        : base(javaReference, transfer) { }

    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        NLogSetupHelper.Setup();
        return base.CustomizeAppBuilder(builder)
            .WithInterFont()
            .UseR3()
            .AfterSetup(appBuilder =>
            {
                var app = (App?)appBuilder.Instance ?? throw new ArgumentException();
                app.ConfiguringServices += static serviceCollection =>
                {
                    serviceCollection.AddSingleton<IAndroidFileService, AndroidFileService>();
                };
            });
    }
}
