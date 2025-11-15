using CommunityToolkit.Mvvm.Messaging.Messages;
using Hoi4BlueprintEditor.Models.Focus;

namespace Hoi4BlueprintEditor.Messages;

public sealed class CreateNewFocusMessage(FocusPoint position) : AsyncRequestMessage<FocusNode>
{
    public FocusPoint Position { get; } = position;
}
