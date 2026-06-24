namespace Hoi4BlueprintBuilder.Core.Messages;

/// <summary>
/// 通知 Focus 完成状态变更
/// </summary>
/// <param name="FocusId">变更的 Focus ID</param>
public sealed record FocusCompletedChangedMessage(string FocusId);
