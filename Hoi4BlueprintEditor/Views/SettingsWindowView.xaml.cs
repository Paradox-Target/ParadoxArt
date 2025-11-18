using System.Diagnostics;
using System.IO;
using System.Windows;
using Hoi4BlueprintEditor.ViewsModels;
using iNKORE.UI.WPF.Modern.Controls;

namespace Hoi4BlueprintEditor.Views;

[RegisterTransient<SettingsWindowView>]
public sealed partial class SettingsWindowView : Window
{
    private readonly SettingsWindowViewModel _viewModel;

    public SettingsWindowView(SettingsWindowViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        DataContext = viewModel;
    }

    private void SettingsWindowView_OnClosed(object? sender, EventArgs e)
    {
        _viewModel.SaveIfChange();
    }

    private void OpenSettingsFolder(object sender, RoutedEventArgs e)
    {
        var process = Process.Start("explorer.exe", $"\"{App.ConfigFolder}\"");
        process.Dispose();
    }
}
