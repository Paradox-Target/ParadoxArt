using System.Collections.Frozen;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Hoi4BlueprintEditor.Extensions;
using Hoi4BlueprintEditor.Models;
using Hoi4BlueprintEditor.Services.GameResources.Base;
using MethodTimer;
using NLog;
using ParadoxPower.CSharpExtensions;
using ParadoxPower.Process;

namespace Hoi4BlueprintEditor.Services.GameResources;

[RegisterSingleton<SpriteService>]
public sealed class SpriteService
    : CommonResourcesService<SpriteService, FrozenDictionary<string, SpriteInfo>>
{
    [Time("加载所有 Sprite 文件")]
    public SpriteService(GameResourcesPathService pathService)
        : base("interface", WatcherFilter.GfxFiles, PathType.Folder, SearchOption.AllDirectories, true)
    {
        _pathService = pathService;
    }

    private ICollection<FrozenDictionary<string, SpriteInfo>> Sprites => Resources.Values;
    private readonly GameResourcesPathService _pathService;

    public bool TryGetSpriteInfo(string spriteTypeName, [NotNullWhen(true)] out SpriteInfo? info)
    {
        foreach (var sprite in Sprites)
        {
            if (sprite.TryGetValue(spriteTypeName, out info))
            {
                return true;
            }
        }

        info = null;
        return false;
    }

    public bool TryGetSpriteFilePath(string spriteTypeName, [NotNullWhen(true)] out string? info)
    {
        if (TryGetSpriteInfo(spriteTypeName, out var spriteInfo))
        {
            info = _pathService.GetFilePathPriorModByRelativePath(spriteInfo.RelativePath);
            return info is not null;
        }
        info = null;
        return false;
    }

    protected override FrozenDictionary<string, SpriteInfo> ParseFileToContent(Node rootNode)
    {
        var sprites = new Dictionary<string, SpriteInfo>(16);

        foreach (var child in rootNode.AllArray)
        {
            if (!(child.TryGetNode(out var spriteTypes) && spriteTypes.Key.EqualsIgnoreCase("spriteTypes")))
            {
                continue;
            }

            foreach (
                var spriteType in spriteTypes.Nodes.Where(node => node.Key.EqualsIgnoreCase("spriteType"))
            )
            {
                ParseSpriteTypeNodeToDictionary(spriteType, sprites);
            }
        }

        return sprites.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    }

    private static void ParseSpriteTypeNodeToDictionary(
        Node spriteTypeNode,
        Dictionary<string, SpriteInfo> sprites
    )
    {
        string? spriteTypeName = null;
        string? textureFilePath = null;
        short frameSum = 1;

        foreach (var leaf in spriteTypeNode.Leaves)
        {
            if (StringComparer.OrdinalIgnoreCase.Equals("name", leaf.Key))
            {
                spriteTypeName = leaf.ValueText;
            }
            else if (StringComparer.OrdinalIgnoreCase.Equals("texturefile", leaf.Key))
            {
                textureFilePath = leaf.ValueText;
            }
            else if (
                StringComparer.OrdinalIgnoreCase.Equals("noOfFrames", leaf.Key)
                && leaf.Value.TryGetIntCast(out int frameSumValue)
            )
            {
                Debug.Assert(frameSumValue <= short.MaxValue);
                frameSum = (short)frameSumValue;
            }
        }

        if (spriteTypeName is null || textureFilePath is null)
        {
            return;
        }

        sprites[spriteTypeName] = new SpriteInfo(spriteTypeName, textureFilePath, frameSum);
    }
}
