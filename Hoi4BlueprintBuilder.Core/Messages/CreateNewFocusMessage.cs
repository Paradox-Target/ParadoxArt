using Hoi4BlueprintBuilder.Core.Models.Focus;

namespace Hoi4BlueprintBuilder.Core.Messages;

public sealed record CreateNewFocusMessage(
    FocusPoint Position,
    string FocusId,
    FocusType FocusType,
    string FocusFilePath
);
