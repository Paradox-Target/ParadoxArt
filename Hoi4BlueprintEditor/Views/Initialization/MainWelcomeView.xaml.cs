using System.Windows.Controls;

namespace Hoi4BlueprintEditor.Views.Initialization;

[RegisterTransient<MainWelcomeView>]
public sealed partial class MainWelcomeView : UserControl
{
    public MainWelcomeView()
    {
        InitializeComponent();

        MainFrame.Navigate(new GameSettingsPageView(MainFrame));
    }
}