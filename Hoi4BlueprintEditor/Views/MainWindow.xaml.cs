using System.Windows;
using CommunityToolkit.Mvvm.Messaging;
using Hoi4BlueprintEditor.Messages;
using Hoi4BlueprintEditor.ViewsModels;

namespace Hoi4BlueprintEditor.Views;

public sealed partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        StateChanged += (sender, _) =>
            WeakReferenceMessenger.Default.Send(new MainWindowStateChangeMessage(sender));
        Deactivated += (sender, _) =>
            WeakReferenceMessenger.Default.Send(new MainWindowDeactivatedMessage(sender));
    }
}
