using FluentAvalonia.UI.Windowing;
using Hoi4BlueprintBuilder.Core.ViewsModels;

namespace Hoi4BlueprintBuilder.Core.Views;

public sealed partial class FocusIconPickerView : FAAppWindow
{
    private FocusIconPickerViewModel? _viewModel;

    public FocusIconPickerView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Closed += OnClosed;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_viewModel is not null)
        {
            _viewModel.RequestClose -= OnRequestClose;
        }

        _viewModel = DataContext as FocusIconPickerViewModel;
        if (_viewModel is not null)
        {
            _viewModel.RequestClose += OnRequestClose;
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
}
