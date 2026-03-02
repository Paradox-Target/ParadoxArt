using Avalonia.Controls;
using Avalonia.Interactivity;
using CommunityToolkit.Mvvm.Messaging;
using Hoi4BlueprintBuilder.Core.Messages;
using Hoi4BlueprintBuilder.Core.ViewsModels;

namespace Hoi4BlueprintBuilder.Core.Views;

public sealed partial class FocusNodeView : UserControl
{
    public FocusNodeView()
    {
        InitializeComponent();
    }

    private void CompletedCheckBox_CheckChanged(object? sender, RoutedEventArgs e)
    {
        if (DataContext is FocusNodeViewModel vm)
        {
            StrongReferenceMessenger.Default.Send(new FocusCompletedChangedMessage(vm.Node.Id));
        }
    }
}
