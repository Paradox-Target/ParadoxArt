using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Hoi4BlueprintBuilder.Core.Helpers;
using Hoi4BlueprintBuilder.Core.Services;
using LiveMarkdown.Avalonia;
using Microsoft.Extensions.DependencyInjection;

namespace Hoi4BlueprintBuilder.Core.Views;

[RegisterTransient<EulaView>]
public sealed partial class EulaView : UserControl
{
    public EulaView()
    {
        InitializeComponent();

        var sb = new ObservableStringBuilder();
        MarkdownRenderer.MarkdownBuilder = sb;
        sb.Append(AssetLoadHelper.GetContentText("EULA.txt"));
    }

    private void ExitApp(object? sender, RoutedEventArgs e)
    {
        var lifetime = App.Current.ApplicationLifetime;

        if (lifetime is IClassicDesktopStyleApplicationLifetime desktopLifetime)
        {
            // Windows / Linux / macOS 桌面端
            desktopLifetime.Shutdown();
        }
        else if (lifetime is ISingleViewApplicationLifetime)
        {
            // Android / iOS 移动端
            // TODO: 移动端的正确退出方式
            Environment.Exit(0);
        }
    }

    private void NextStep(object? sender, RoutedEventArgs e)
    {
        var navigationService = App.Current.Services.GetRequiredService<NavigationService>();
        var settings = App.Current.Services.GetRequiredService<SettingsService>();
        settings.IsAgreedEula = true;
        navigationService.NavigateBasedOnDeviceStatus();
    }
}
