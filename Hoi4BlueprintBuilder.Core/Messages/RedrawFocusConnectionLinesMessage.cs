namespace Hoi4BlueprintBuilder.Core.Messages;

/// <summary>
/// 通知重绘 Focus 之间的连接线
/// </summary>
public sealed class RedrawFocusConnectionLinesMessage
{
    public static readonly RedrawFocusConnectionLinesMessage Instance = new();

    private RedrawFocusConnectionLinesMessage() { }
}
