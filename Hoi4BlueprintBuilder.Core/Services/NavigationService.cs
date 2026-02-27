using Avalonia.Threading;
using Hoi4BlueprintBuilder.Core.Views;
using Hoi4BlueprintBuilder.Core.Views.Initialization;
using Microsoft.Extensions.DependencyInjection;
using NLog;

namespace Hoi4BlueprintBuilder.Core.Services;

[RegisterSingleton<NavigationService>]
public sealed class NavigationService(IServiceProvider serviceProvider)
{
    /// <summary>
    /// 当前视图改变时触发, 传递参数为当前视图
    /// </summary>
    public event Action<object?>? ViewChanged;

    public object? CurrentView
    {
        get;
        private set
        {
            field = value;
            ViewChanged?.Invoke(value);
        }
    }

    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public void NavigateTo<TView>()
    {
        NavigateTo(typeof(TView));
    }

    private void NavigateTo(Type type)
    {
        Dispatcher.UIThread.Post(() =>
        {
            CurrentView = serviceProvider.GetRequiredService(type);
        });
        Log.Info("导航到 {Name}", type.Name);
    }

    public void NavigateBasedOnDeviceStatus()
    {
        var settingsService = serviceProvider.GetRequiredService<SettingsService>();
        var messageBox = serviceProvider.GetRequiredService<MessageBoxService>();

        if (!settingsService.IsAgreedEula)
        {
            NavigateTo<EulaView>();
        }
        else if (settingsService.IsFirstRun)
        {
            NavigateTo<MainWelcomeView>();
        }
        else
        {
            if (Directory.Exists(settingsService.GameRootFolderPath))
            {
                NavigateTo<ProjectListView>();
            }
            else
            {
                messageBox.ShowAsync("游戏文件夹路径无效，请重新设置", "路径无效", MessageBoxIcon.Warning);
                NavigateTo<MainWelcomeView>();
            }
        }
    }
}
