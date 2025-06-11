using System.Configuration;
using System.Data;
using System.Globalization;
using System.Windows;
using Hoi4BlueprintEditor.ViewModels;
using Hoi4BlueprintEditor.Views;
using Microsoft.Extensions.DependencyInjection;

namespace Hoi4BlueprintEditor;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public static new App Current => (App)Application.Current;
    public IServiceProvider Services { get; } = ConfigureServices();

    private MainWindow _main = null!;

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        services.AddSingleton<MainWindow>();
        services.AddSingleton<MainWindowViewModel>();

        return services.BuildServiceProvider();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _main = Services.GetRequiredService<MainWindow>();
        _main.ShowDialog();
    }
}
