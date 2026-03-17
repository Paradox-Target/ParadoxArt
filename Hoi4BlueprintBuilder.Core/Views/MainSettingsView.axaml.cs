using Avalonia.Controls;
using FluentAvalonia.UI.Controls;
using Microsoft.Extensions.DependencyInjection;
using FontIconSource = Hoi4BlueprintBuilder.Core.Controls.FontIconSource;

namespace Hoi4BlueprintBuilder.Core.Views;

[RegisterTransient<MainSettingsView>]
public sealed partial class MainSettingsView : UserControl, ITabViewItem
{
    public string Header => "设置";
    public string FilePath => "SettingsView";
    public string ToolTip => Header;
    public IconSource TabIcon { get; } = new FontIconSource { Glyph = "\uE713" };

    private readonly IServiceProvider _serviceProvider;

    public MainSettingsView()
    {
        InitializeComponent();
        _serviceProvider = null!;
    }

    public MainSettingsView(ProjectSettingsView projectSettingsView, IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        InitializeComponent();
        ProjectSettingsTab.Content = projectSettingsView;
        SettingsTabs.SelectionChanged += SettingsTabsOnSelectionChanged;
    }

    private void SettingsTabsOnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems[0] is not TabItem current)
        {
            return;
        }

        if (current.Content is not null)
        {
            return;
        }

        if (ReferenceEquals(AppSettingsTab, current))
        {
            current.Content = _serviceProvider.GetRequiredService<AppSettingsView>();
        }
    }
}
