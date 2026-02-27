using System.Text;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Platform;
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

        using var reader = new StreamReader(
            AssetLoader.Open(new Uri("avares://ParadoxArt.Core/Assets/EULA.txt")),
            Encoding.UTF8
        );
        var sb = new ObservableStringBuilder();
        MarkdownRenderer.MarkdownBuilder = sb;
        sb.Append(reader.ReadToEnd());
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
