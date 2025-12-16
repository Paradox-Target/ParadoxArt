using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Hoi4BlueprintBuilder.Core.ViewsModels.Initialization;
using Microsoft.Extensions.DependencyInjection;

namespace Hoi4BlueprintBuilder.Core.Views.Initialization;

public partial class GameSettingsPageView : UserControl
{
    public GameSettingsPageView(Frame frame)
    {
        InitializeComponent();

        var viewModel = App.Current.Services.GetRequiredService<GameSettingsPageViewModel>();
        viewModel.Frame = frame;
        DataContext = viewModel;
    }
}