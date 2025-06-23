namespace Hoi4BlueprintEditor.Services.GameResources.Base;

public abstract partial class ResourcesService<TType, TContent, TParseResult>
{
    protected enum PathType : byte
    {
        File,
        Folder
    }
}