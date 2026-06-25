using Avalonia.Controls;
using Avalonia.Interactivity;
using Hoi4BlueprintBuilder.Core.Messages;
using Hoi4BlueprintBuilder.Core.ViewsModels;
using MessagePipe;
using Microsoft.Extensions.DependencyInjection;

namespace Hoi4BlueprintBuilder.Core.Views;

public sealed partial class FocusNodeView : UserControl
{
    // 不使用 static 字段是是因为与热重载冲突
    private readonly IPublisher<FocusCompletedChangedMessage> _focusCompletedChangedPublisher =
        App.Current.Services.GetRequiredService<IPublisher<FocusCompletedChangedMessage>>();

    public FocusNodeView()
    {
        InitializeComponent();
    }

    private void CompletedCheckBox_CheckChanged(object? sender, RoutedEventArgs e)
    {
        if (DataContext is FocusNodeViewModel vm)
        {
            _focusCompletedChangedPublisher.Publish(new FocusCompletedChangedMessage(vm.Node.Id));
        }
    }
}
