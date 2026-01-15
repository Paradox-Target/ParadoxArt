using CommunityToolkit.Mvvm.Messaging.Messages;
using Hoi4BlueprintBuilder.Core.Models.Focus;

namespace Hoi4BlueprintBuilder.Core.Messages;

/// <summary>
/// StrongReferenceMessenger
/// </summary>
/// <param name="position"></param>
/// <param name="focusId"></param>
/// <param name="focusType"></param>
/// <param name="focusFilePath"></param>
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
