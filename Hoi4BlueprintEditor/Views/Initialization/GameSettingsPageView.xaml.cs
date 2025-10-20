using System.Windows.Controls;
using Hoi4BlueprintEditor.ViewsModels.Initialization;
using Microsoft.Extensions.DependencyInjection;

namespace Hoi4BlueprintEditor.Views.Initialization;

public sealed partial class GameSettingsPageView : Page
{
    public GameSettingsPageView(Frame frame)
    {
        InitializeComponent();

        var viewModel = App.Current.Services.GetRequiredService<GameSettingsPageViewModel>();
        viewModel.Frame = frame;
        DataContext = viewModel;
    }
}
