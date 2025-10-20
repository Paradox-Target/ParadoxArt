using System.Windows;
using System.Windows.Controls;
using Hoi4BlueprintEditor.Helpers;
using Hoi4BlueprintEditor.Services;
using Hoi4BlueprintEditor.ViewsModels.Initialization;
using Microsoft.Extensions.DependencyInjection;

namespace Hoi4BlueprintEditor.Views.Initialization;

public sealed partial class AppSettingsPageView : Page
{
    private readonly Frame _frame;

    public AppSettingsPageView(Frame frame)
    {
        _frame = frame;
        InitializeComponent();

        DataContext = App.Current.Services.GetRequiredService<AppSettingsPageViewModel>();
    }

    private void Previous_OnClick(object sender, RoutedEventArgs e)
    {
        _frame.GoBack();
    }

    private void Next_OnClick(object sender, RoutedEventArgs e)
    {
        var settingsService = App.Current.Services.GetRequiredService<SettingsService>();
        LanguageHelper.SetLanguage(settingsService.Language);
        settingsService.SaveSettings();
        App.Current.Services.GetRequiredService<NavigationService>().NavigateTo<MainControlView>();
    }
}
