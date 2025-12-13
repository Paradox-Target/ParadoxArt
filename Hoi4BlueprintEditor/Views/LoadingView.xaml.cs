using System.Windows;
using System.Windows.Controls;
using Hoi4BlueprintEditor.Services;
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
                if (task.IsCompletedSuccessfully && task.Result)
                {
                    navigationService.NavigateTo<MainControlView>();
                }
                else
                {
                    navigationService.NavigateTo<ActivateWindowView>();
                }
            });
        });
    }
}
