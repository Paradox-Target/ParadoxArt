using System.Windows.Controls;
using Hoi4BlueprintEditor.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Hoi4BlueprintEditor.Views;

[RegisterTransient<LoadingView>]
public sealed partial class LoadingView : UserControl
{
    public LoadingView()
    {
        InitializeComponent();
        App.Current.IsActivated.ContinueWith(async task =>
        {
            Dispatcher.Invoke(() =>
            {
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
