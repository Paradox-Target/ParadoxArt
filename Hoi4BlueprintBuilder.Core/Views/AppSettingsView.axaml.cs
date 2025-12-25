using Avalonia.Controls;
using FluentAvalonia.UI.Controls;
using Hoi4BlueprintBuilder.Core.ViewsModels;
using FontIconSource = Hoi4BlueprintBuilder.Core.Controls.FontIconSource;

namespace Hoi4BlueprintBuilder.Core.Views;

[RegisterTransient<AppSettingsView>]
public sealed partial class AppSettingsView : UserControl, ITabViewItem
{
    public string Header => "应用设置";
    public string FilePath => "应用设置";
    public string ToolTip => "应用设置";
    public IconSource TabIcon { get; } = new FontIconSource { Glyph = "\uE713" };

    private readonly AppSettingsViewModel _viewModel;

    public AppSettingsView(AppSettingsViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        DataContext = viewModel;

        Unloaded += OnUnloaded;
    }

    private void OnUnloaded(object? sender, EventArgs e)
    {
        _viewModel.SaveIfChange();
    }
}
