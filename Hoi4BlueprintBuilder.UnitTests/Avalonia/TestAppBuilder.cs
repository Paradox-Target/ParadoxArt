using Avalonia;
using Avalonia.Headless;
using Hoi4BlueprintBuilder.UnitTests.Avalonia;

[assembly: AvaloniaTestApplication(typeof(TestAppBuilder))]

namespace Hoi4BlueprintBuilder.UnitTests.Avalonia;

public sealed class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>().UseHeadless(new AvaloniaHeadlessPlatformOptions());
}
