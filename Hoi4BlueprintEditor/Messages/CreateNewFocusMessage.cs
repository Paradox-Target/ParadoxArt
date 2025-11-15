using CommunityToolkit.Mvvm.Messaging.Messages;
using Hoi4BlueprintEditor.Models.Focus;

namespace Hoi4BlueprintEditor.Messages;

public sealed class CreateNewFocusMessage(Point position) : AsyncRequestMessage<FocusNode>
{
    public Point Position { get; } = position;
}
