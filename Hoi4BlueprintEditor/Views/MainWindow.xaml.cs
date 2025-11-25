using System.Windows;
using Hoi4BlueprintEditor.Services;
using Hoi4BlueprintEditor.ViewsModels;

namespace Hoi4BlueprintEditor.Views;

public sealed partial class MainWindow : Window
{
    private readonly WindowSettingsService _windowSettingsService;

    public MainWindow(MainWindowViewModel viewModel, WindowSettingsService windowSettingsService)
    {
        _windowSettingsService = windowSettingsService;
        InitializeComponent();
        DataContext = viewModel;
        
        windowSettingsService.SetWindow(this);
    }

    private void MainWindow_OnClosed(object? sender, EventArgs e)
    {
        _windowSettingsService.SaveWindow(this);
    }
}
