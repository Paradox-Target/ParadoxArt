using Avalonia.Controls;
using Hoi4BlueprintBuilder.Core.ViewsModels;
using Microsoft.Extensions.DependencyInjection;

namespace Hoi4BlueprintBuilder.Core.Views;

public sealed partial class TitleCommandBarView : UserControl
{
    public TitleCommandBarView()
    {
        InitializeComponent();
        DataContext = App.Current.Services.GetRequiredService<TitleCommandBarViewModel>();
    }
}
