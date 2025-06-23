namespace Hoi4BlueprintEditor.Services.GameResources.Base;

public sealed class ResourceChangedEventArgs(string filePath) : EventArgs
{
    public string FilePath { get; } = filePath;
}