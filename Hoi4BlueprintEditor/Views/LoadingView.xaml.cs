using System.Windows;
using System.Windows.Controls;
using Hoi4BlueprintEditor.Services;
using Hoi4BlueprintEditor.Views.Initialization;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using MessageBox = iNKORE.UI.WPF.Modern.Controls.MessageBox;

namespace Hoi4BlueprintEditor.Views;

[RegisterTransient<LoadingView>]
public sealed partial class LoadingView : UserControl
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public LoadingView()
    {
        InitializeComponent();
        App.Current.IsActivated.ContinueWith(task =>
        {
            Dispatcher.Invoke(async () =>
            {
                if (task.Exception is not null)
                {
                    Log.Error(task.Exception, "查询设备激活状态失败");
                    await MessageBox.ShowAsync(
                        App.Current.MainWindow,
                        "查询设备激活状态失败，请检查网络并稍后重试, 软件将自动关闭.",
                        "错误",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                    );
                    App.Current.Shutdown();
                    return;
                }

                var navigationService = App.Current.Services.GetRequiredService<NavigationService>();
                var settingsService = App.Current.Services.GetRequiredService<SettingsService>();
                if (task.IsCompletedSuccessfully && task.Result)
                {
                    if (settingsService.IsFirstRun)
                    {
                        navigationService.NavigateTo<MainWelcomeView>();
                    }
                    else
                    {
                        if (
                            Directory.Exists(settingsService.GameRootFolderPath)
                            && Directory.Exists(settingsService.ModRootFolderPath)
                        )
                        {
                            navigationService.NavigateTo<MainControlView>();
                        }
                        else
                        {
                            MessageBox.Show(
                                "游戏或MOD文件夹路径无效，请重新设置",
                                "路径无效",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning
                            );
                            navigationService.NavigateTo<MainWelcomeView>();
                        }
                    }
                }
                else
                {
                    navigationService.NavigateTo<ActivateWindowView>();
                }
            });
        });
    }
}
