using Microsoft.Extensions.DependencyInjection;

namespace Hoi4BlueprintBuilder.Core.Services;

[RegisterSingleton<NavigationService>]
public sealed class NavigationService
{
    public event Action? ViewChanged;

    public object? CurrentView
    {
        get;
        private set
        {
            field = value;
            ViewChanged?.Invoke();
        }
    }

    public void NavigateTo<TView>()
    {
        NavigateTo(typeof(TView));
    }

    private void NavigateTo(Type type)
    {
        CurrentView = App.Current.Services.GetRequiredService(type);
    }
}
