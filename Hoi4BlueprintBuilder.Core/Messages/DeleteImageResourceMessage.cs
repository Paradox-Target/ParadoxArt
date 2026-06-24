namespace Hoi4BlueprintBuilder.Core.Messages;

/// <summary>
/// MessagePipe 发布/订阅消息：通知删除指定 Sprite 的图像资源
/// </summary>
/// <param name="spriteName"></param>
public sealed class DeleteImageResourceMessage(string spriteName)
{
    public string SpriteName { get; } = spriteName;
}
