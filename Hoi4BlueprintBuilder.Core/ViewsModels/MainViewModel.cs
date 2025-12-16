using CommunityToolkit.Mvvm.ComponentModel;

namespace Hoi4BlueprintBuilder.Core.ViewsModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private string _greeting = "Welcome to Avalonia!";
}
