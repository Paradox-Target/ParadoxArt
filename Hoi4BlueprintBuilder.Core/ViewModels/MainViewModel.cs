using CommunityToolkit.Mvvm.ComponentModel;

namespace Hoi4BlueprintBuilder.Core.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty] private string _greeting = "Welcome to Avalonia!";
}