using System.Collections.ObjectModel;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Controls.Primitives;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using DynamicData.Aggregation;
using Hoi4BlueprintBuilder.Core.Models;
using Hoi4BlueprintBuilder.Core.Models.Focus;
using Hoi4BlueprintBuilder.Core.Services;
using Hoi4BlueprintBuilder.Core.Services.GameResources;
using Hoi4BlueprintBuilder.Localization.Strings;
using R3;
using ZLinq;

namespace Hoi4BlueprintBuilder.Core.ViewsModels;

[RegisterSingleton<FocusIconPickerViewModel>]
public sealed partial class FocusIconPickerViewModel : ObservableObject, IDisposable
{
    public static string AllIconsKey => LangResources.FocusIconFavorites_All;
    private readonly SpriteService _spriteService;
    private readonly ProjectConfigService _projectConfigService;
    private readonly MessageBoxService _messageBoxService;
    private readonly NotificationService _notificationService;
    public string SelectedFocusIcon { get; private set; } = string.Empty;
    public ReadOnlyObservableCollection<FocusIcon> FocusIcons => _focusIcons;
    private readonly ReadOnlyObservableCollection<FocusIcon> _focusIcons;

    [ObservableProperty]
    public partial string Sum { get; private set; } = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RemoveIconFromFavoritesCommand))]
    public partial string CurrentFavoritesName { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CreateNewIconFavoritesCommand))]
    public partial string NewIconFavoritesName { get; set; } = string.Empty;
    private bool IsCanCreateNewIconFavorites => !string.IsNullOrWhiteSpace(NewIconFavoritesName);

    public ObservableCollection<string> FavoritesNames { get; }
    public FocusIcon? CurrentIcon { get; set; }

    public BindableReactiveProperty<string> SearchText { get; } = new(string.Empty);

    private bool CanRemoveIconFromFavorites => CurrentIcon is not null && CurrentFavoritesName != AllIconsKey;
    private readonly SourceList<FocusIcon> _focusIconsSource = new();
    private ToggleButton? _selectedIconButton;
    private readonly IDisposable _disposable;

    public FocusIconPickerViewModel(
        SpriteService spriteService,
        ProjectConfigService projectConfigService,
        MessageBoxService messageBoxService,
        NotificationService notificationService
    )
    {
        _spriteService = spriteService;
        _projectConfigService = projectConfigService;
        _messageBoxService = messageBoxService;
        _notificationService = notificationService;
        FavoritesNames = new ObservableCollection<string>(
            _projectConfigService.IconFavorites.Select(favorites => favorites.Name).Prepend(AllIconsKey)
        );
        CurrentFavoritesName = AllIconsKey;
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
            icon =>
                string.IsNullOrEmpty(text) || icon.Name.Contains(text, StringComparison.OrdinalIgnoreCase);
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

    [RelayCommand(CanExecute = nameof(IsCanCreateNewIconFavorites))]
    private void CreateNewIconFavorites()
    {
        Debug.Assert(!string.IsNullOrEmpty(NewIconFavoritesName));

        if (FavoritesNames.Contains(NewIconFavoritesName))
        {
            _ = _messageBoxService.ShowErrorAsync(
                LangResources.FocusIconFavorites_FavoritesAlreadyExists,
                LangResources.Common_Error
            );
            return;
        }

        _projectConfigService.IconFavorites.Add(new IconFavorites(NewIconFavoritesName, []));
        FavoritesNames.Add(NewIconFavoritesName);
        NewIconFavoritesName = string.Empty;
    }

    [RelayCommand]
    private async Task DeleteIconFavorites()
    {
        string favoritesName = CurrentFavoritesName;
        if (string.IsNullOrEmpty(favoritesName))
        {
            return;
        }

        if (favoritesName == AllIconsKey)
        {
            _ = _messageBoxService.ShowErrorAsync(
                LangResources.FocusIconFavorites_CanNotDeleteDefaultFavorites,
                LangResources.Common_Error
            );
            return;
        }

        var favorites = _projectConfigService
            .IconFavorites.AsValueEnumerable()
            .FirstOrDefault(favorites => favorites.Name == favoritesName);
        if (favorites is null)
        {
            _ = _messageBoxService.ShowErrorAsync(
                LangResources.FocusIconFavorites_NotFound,
                LangResources.Common_Error
            );
            return;
        }

        if (
            await _messageBoxService.ShowAsync(
                string.Format(LangResources.FocusIconFavorites_DeleteConfirm, favoritesName),
                LangResources.Common_ConfirmDelete,
                MessageBoxIcon.Question,
                MessageBoxButtons.YesNo
            ) != MessageBoxResult.Yes
        )
        {
            return;
        }

        _projectConfigService.IconFavorites.Remove(favorites);
        FavoritesNames.Remove(favoritesName);
    }

    partial void OnCurrentFavoritesNameChanged(string value)
    {
        SetCurrentFavorites(value);
    }

    private void SetCurrentFavorites(string favoritesName)
    {
        if (string.IsNullOrEmpty(favoritesName))
        {
            return;
        }

        if (favoritesName == AllIconsKey)
        {
            _focusIconsSource.Clear();
            AddAllFocusIcons();
            return;
        }

        var favorites = _projectConfigService
            .IconFavorites.AsValueEnumerable()
            .FirstOrDefault(favorites => favorites.Name == favoritesName);
        if (favorites is null)
        {
            _messageBoxService.ShowErrorAsync(
                LangResources.FocusIconFavorites_NotFound,
                LangResources.Common_Error
            );
            return;
        }

        _focusIconsSource.Clear();
        _focusIconsSource.AddRange(favorites.Icons.Select(name => new FocusIcon(name)));
    }

    private void AddAllFocusIcons()
    {
        _focusIconsSource.AddRange(_spriteService.GetAllFocusIconNames().Select(name => new FocusIcon(name)));
    }

    public void AddIconToFavorites(string favoritesName)
    {
        if (string.IsNullOrEmpty(favoritesName) || CurrentIcon is null)
        {
            return;
        }

        var iconFavorites = _projectConfigService.IconFavorites.FirstOrDefault(favorites =>
            favorites.Name == favoritesName
        );

        Debug.Assert(iconFavorites is not null);
        if (iconFavorites.Icons.Contains(CurrentIcon.Name, StringComparer.OrdinalIgnoreCase))
        {
            _notificationService.Show(
                LangResources.FocusIconFavorites_IconAlreadyExists,
                CurrentIcon.Name,
                NotificationType.Warning
            );
            return;
        }
        iconFavorites.Icons.Add(CurrentIcon.Name);
        _notificationService.Show(
            LangResources.FocusIconPicker_AddIconToFavoritesSuccess,
            CurrentIcon.Name,
            NotificationType.Success
        );
    }

    [RelayCommand(CanExecute = nameof(CanRemoveIconFromFavorites))]
    private void RemoveIconFromFavorites()
    {
        if (CurrentIcon is null)
        {
            return;
        }

        string iconName = CurrentIcon.Name;
        _focusIconsSource.Remove(CurrentIcon);
        _projectConfigService
            .IconFavorites.FirstOrDefault(favorites => favorites.Name == CurrentFavoritesName)
            ?.Icons.Remove(iconName);
    }

    public event Action<bool>? RequestClose;

    [RelayCommand]
    private void Ok() => RequestClose?.Invoke(true);

    [RelayCommand]
    private void Cancel() => RequestClose?.Invoke(false);

    public void Dispose()
    {
        RequestClose = null;
        _focusIconsSource.Dispose();
        _disposable.Dispose();
        SearchText.Dispose();
    }
}
