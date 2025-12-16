using Avalonia.Controls;

namespace Hoi4BlueprintBuilder.Core.Views.Initialization;

[RegisterTransient<MainWelcomeView>]
public sealed partial class MainWelcomeView : UserControl
{
    public MainWelcomeView()
    {
        InitializeComponent();

        MainFrame.Navigate(new GameSettingsPageView(MainFrame));
    }
}