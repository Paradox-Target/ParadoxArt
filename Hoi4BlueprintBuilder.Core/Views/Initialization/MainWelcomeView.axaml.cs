using Avalonia.Controls;
using FluentAvalonia.UI.Controls;

namespace Hoi4BlueprintBuilder.Core.Views.Initialization;

[RegisterTransient<MainWelcomeView>]
public sealed partial class MainWelcomeView : UserControl
{
    public MainWelcomeView()
    {
        InitializeComponent();

        MainFrame.NavigationPageFactory = new NavigationPageFactory();
        MainFrame.NavigateFromObject(new GameSettingsPageView(MainFrame));
    }

    private sealed class NavigationPageFactory : IFANavigationPageFactory
    {
        public Control GetPage(Type srcType)
        {
            throw new NotImplementedException();
        }

        public Control GetPageFromObject(object target)
        {
            return (Control)target;
        }
    }
}
