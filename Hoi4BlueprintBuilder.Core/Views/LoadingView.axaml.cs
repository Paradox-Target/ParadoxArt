using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using Hoi4BlueprintBuilder.Core.Services;
using NLog;

namespace Hoi4BlueprintBuilder.Core.Views;

[RegisterTransient<LoadingView>]
public sealed partial class LoadingView : UserControl
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public LoadingView(NavigationService navigationService, MessageBoxService messageBox)
    {
        InitializeComponent();
        App.Current.IsActivated?.ContinueWith(async task =>
        {
            if (task.Exception is not null)
            {
                Log.Error(task.Exception, "查询设备激活状态失败");
                await messageBox.ShowAsync("查询设备激活状态失败，请检查网络并稍后重试, 软件将自动关闭.", "错误", MessageBoxIcon.Error);
                ExitApplication();
                return;
            }

            navigationService.NavigateBasedOnDeviceStatus();
        });
    }

    public static void ExitApplication()
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
}
