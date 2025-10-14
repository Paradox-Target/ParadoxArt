using Microsoft.Extensions.DependencyInjection;

namespace Hoi4BlueprintEditor.Services;

[RegisterSingleton<NavigationService>]
public sealed class NavigationService
{
    public object? CurrentView
    {
        get => _currentView;
        private set
        {
            _currentView = value;
            ViewChanged?.Invoke();
        }
    }

    public event Action? ViewChanged;
    private object? _currentView;
    public void NavigateTo<TView>()
    {
        NavigateTo(typeof(TView));
    }

    private void NavigateTo(Type type)
    {
        CurrentView = App.Current.Services.GetRequiredService(type);
    }
}