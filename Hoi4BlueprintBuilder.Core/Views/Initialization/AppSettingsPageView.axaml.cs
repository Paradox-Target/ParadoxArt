using Avalonia.Controls;
using Avalonia.Interactivity;
using FluentAvalonia.UI.Controls;
using Hoi4BlueprintBuilder.Core.Helpers;
using Hoi4BlueprintBuilder.Core.Services;
using Hoi4BlueprintBuilder.Core.ViewsModels.Initialization;
using Microsoft.Extensions.DependencyInjection;

namespace Hoi4BlueprintBuilder.Core.Views.Initialization;

public sealed partial class AppSettingsPageView : UserControl
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
        LanguageHelper.SetLanguage(settingsService.AppLanguage);
        settingsService.SaveSettings();
        App.Current.Services.GetRequiredService<NavigationService>().NavigateTo<MainView>();
    }
}
