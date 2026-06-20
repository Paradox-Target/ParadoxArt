using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using DynamicData.Aggregation;
using Hoi4BlueprintBuilder.Core.Models.Focus;
using Hoi4BlueprintBuilder.Core.Services.GameResources;
using Hoi4BlueprintBuilder.Localization.Strings;
using R3;

namespace Hoi4BlueprintBuilder.Core.ViewsModels;

[RegisterSingleton<FocusIconPickerViewModel>]
public sealed partial class FocusIconPickerViewModel : ObservableObject, IDisposable
{
    public string SelectedFocusIcon { get; private set; } = string.Empty;
    public ReadOnlyObservableCollection<FocusIcon> FocusIcons => _focusIcons;
    private readonly ReadOnlyObservableCollection<FocusIcon> _focusIcons;

    [ObservableProperty]
    public partial string Sum { get; private set; } = string.Empty;
    public BindableReactiveProperty<string> SearchText { get; } = new(string.Empty);
    private readonly SourceList<FocusIcon> _focusIconsSource = new();
    private ToggleButton? _selectedIconButton;
    private readonly IDisposable _disposable;

    public FocusIconPickerViewModel(SpriteService spriteService)
    {
        _focusIconsSource.AddRange(spriteService.GetAllFocusIconNames().Select(name => new FocusIcon(name)));
        var predicate = SearchText
            .Debounce(TimeSpan.FromMilliseconds(250))
            .ObserveOnUIThreadDispatcher()
            .DistinctUntilChanged()
            .Select(Filter)
            .AsSystemObservable();
        var filteredConnection = _focusIconsSource.Connect().RefCount().Filter(predicate);
        _disposable = Disposable.Combine(
            filteredConnection.Bind(out _focusIcons).Subscribe(),
            filteredConnection
                .Count()
                .Subscribe(sum => Sum = string.Format(LangResources.FocusIconSum, sum.ToString()))
        );
        return;

        Func<FocusIcon, bool> Filter(string text) =>
            icon => string.IsNullOrEmpty(text) || icon.Name.Contains(text);
    }

    [RelayCommand]
    private void PickFocusIcon(ToggleButton? toggleButton)
    {
        if (ReferenceEquals(_selectedIconButton, toggleButton) || toggleButton is null)
        {
            return;
        }

        _selectedIconButton?.IsChecked = false;
        _selectedIconButton = toggleButton;
        if (toggleButton.Content is Image { DataContext: FocusIcon focusIcon })
        {
            SelectedFocusIcon = focusIcon.Name;
        }
    }

    public void Dispose()
    {
        _focusIconsSource.Dispose();
        _disposable.Dispose();
        SearchText.Dispose();
    }
}
