using Avalonia.Controls;
using Avalonia.Interactivity;
using Hoi4BlueprintBuilder.Core.Messages;
using Hoi4BlueprintBuilder.Core.ViewsModels;
using MessagePipe;
using Microsoft.Extensions.DependencyInjection;

namespace Hoi4BlueprintBuilder.Core.Views;

public sealed partial class FocusNodeView : UserControl
{
    private static readonly IPublisher<FocusCompletedChangedMessage> FocusCompletedChangedPublisher =
        App.Current.Services.GetRequiredService<IPublisher<FocusCompletedChangedMessage>>();

    public FocusNodeView()
    {
        InitializeComponent();
    }

    private void CompletedCheckBox_CheckChanged(object? sender, RoutedEventArgs e)
    {
        if (DataContext is FocusNodeViewModel vm)
        {
            FocusCompletedChangedPublisher.Publish(new FocusCompletedChangedMessage(vm.Node.Id));
        }
    }
}
