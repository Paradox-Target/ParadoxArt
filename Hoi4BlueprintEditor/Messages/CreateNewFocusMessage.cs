using CommunityToolkit.Mvvm.Messaging.Messages;
using Hoi4BlueprintEditor.Models.Focus;

namespace Hoi4BlueprintEditor.Messages;

public sealed class CreateNewFocusMessage(
    FocusPoint position,
    string focusId,
    FocusType focusType,
    string focusFilePath
) : AsyncRequestMessage<FocusNode>
{
    public FocusPoint Position { get; } = position;
    public string FocusId { get; } = focusId;
    public FocusType FocusType { get; } = focusType;
    public string FocusFilePath { get; } = focusFilePath;
}
