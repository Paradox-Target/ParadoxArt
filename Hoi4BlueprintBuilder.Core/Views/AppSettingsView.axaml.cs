using Avalonia.Controls;
using Hoi4BlueprintBuilder.Core.Services;
using Hoi4BlueprintBuilder.Core.ViewsModels;

namespace Hoi4BlueprintBuilder.Core.Views;

[RegisterTransient<AppSettingsView>]
public sealed partial class AppSettingsView : UserControl
{
    private readonly AppSettingsViewModel _viewModel;

    public AppSettingsView()
    {
        InitializeComponent();
        _viewModel = null!;
    }

    public AppSettingsView(AppSettingsViewModel viewModel, TelemetryService telemetryService)
    {
        InitializeComponent();

        _viewModel = viewModel;
        DataContext = viewModel;

        Unloaded += OnUnloaded;
        telemetryService.TrackEvent("Open_AppSettingsView");
    }

    private void OnUnloaded(object? sender, EventArgs e)
    {
        _viewModel.SaveIfChange();
    }
}
