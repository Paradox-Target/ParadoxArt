using Avalonia.Controls;
using Avalonia.Input;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Windowing;
using Hoi4BlueprintBuilder.Core.Models.Focus;
using Hoi4BlueprintBuilder.Core.ViewsModels;

namespace Hoi4BlueprintBuilder.Core.Views;

public sealed partial class FocusIconPickerView : FAAppWindow
{
    private FocusIconPickerViewModel? _viewModel;
    private readonly FAMenuFlyout _contextMenu;

    public FocusIconPickerView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Closed += OnClosed;
        _contextMenu =
            Resources["ContextMenu"] as FAMenuFlyout
            ?? throw new InvalidOperationException("Icon 选择器上下文菜单实例未找到");
        if (Design.IsDesignMode)
        {
            FavoritesListBox.ItemsSource = Enumerable.Range(1, 100).ToArray();
        }
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_viewModel is not null)
        {
            _viewModel.RequestClose -= OnRequestClose;
            RemoveIconFromFavoritesMenuItem.Command = null;
        }

        _viewModel = DataContext as FocusIconPickerViewModel;
        if (_viewModel is not null)
        {
            _viewModel.RequestClose += OnRequestClose;
            RemoveIconFromFavoritesMenuItem.Command = _viewModel.RemoveIconFromFavoritesCommand;
        }
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        DataContextChanged -= OnDataContextChanged;
        if (_viewModel is not null)
        {
            _viewModel.RequestClose -= OnRequestClose;
            _viewModel = null;
        }
    }

    private void OnRequestClose(bool result) => Close(result);

    private void PickButton_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control { DataContext: FocusIcon focusIcon } control)
        {
            return;
        }

        if (_viewModel is null)
        {
            return;
        }

        _viewModel.CurrentIcon = focusIcon;
        var items = _viewModel
            .FavoritesNames.Where(name =>
                name != FocusIconPickerViewModel.AllIconsKey && name != _viewModel.CurrentFavoritesName
            )
            .Select(name => new FAMenuFlyoutItem
            {
                Text = name,
                Command = new RelayCommand(() => _viewModel.AddIconToFavorites(name))
            })
            .ToArray();
        AddToFavoritesMenuItem.ItemsSource = items;
        AddToFavoritesMenuItem.IsEnabled = items.Length != 0;
        _contextMenu.ShowAt(control, true);
    }
}
