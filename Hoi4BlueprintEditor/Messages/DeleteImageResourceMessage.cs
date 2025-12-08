namespace Hoi4BlueprintEditor.Messages;

/// <summary>
/// <see cref="CommunityToolkit.Mvvm.Messaging.StrongReferenceMessenger"/>
/// </summary>
/// <param name="spriteName"></param>
public sealed class DeleteImageResourceMessage(string spriteName)
{
    public string SpriteName { get; } = spriteName;
}