using System.Diagnostics;
using BCnEncoder.Encoder;
using BCnEncoder.ImageSharp;
using BCnEncoder.Shared;
using Hoi4BlueprintBuilder.Core.Extensions;
using Hoi4BlueprintBuilder.Core.Helpers;
using Hoi4BlueprintBuilder.Core.Models;
using ParadoxPower.CSharpExtensions;
using ParadoxPower.Parser;
using ParadoxPower.Process;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using ZLinq;

namespace Hoi4BlueprintBuilder.Core.Services;

[RegisterSingleton<FileResourceService>]
public sealed class FileResourceService(SettingsService settingsService)
{
    private const string SpriteTypeKey = "SpriteType";
    private const string SpriteTypesKey = "spriteTypes";

    /// <summary>
    /// 注册 Focus 图标, 并返回图标名称和图标文件路径
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <remarks>仅支持 Png 格式和 Dds 格式, Png 会被转变为Dds格式(如果启用自动转码设置的话)</remarks>
    /// <returns>注册结果</returns>
    public RegisterFocusIconResult RegisterFocusIcon(string filePath)
    {
        Debug.Assert(ImageHelper.IsValidFocusImageFormat(filePath));

        // 注册文件中的Icon相对路径
        string relativePath = Path.Combine(
            "gfx",
            "editor",
            "interface",
            "focusIcon",
            Path.GetFileName(filePath)
        );
        string destPath = Path.Combine(settingsService.ModRootFolderPath, relativePath);
        _ = Directory.CreateDirectory(Path.GetDirectoryName(destPath) ?? string.Empty);

        bool isNeedConvertToDds =
            settingsService.IsAutoFocusPngConvertToDds
            && Path.GetExtension(filePath.AsSpan())
                .Equals(".png", StringComparison.OrdinalIgnoreCase);

        // 复制图片到 mod 文件夹中
        if (isNeedConvertToDds)
        {
            destPath = Path.ChangeExtension(destPath, ".dds");
            relativePath = Path.ChangeExtension(relativePath, ".dds");
            PngToDds(filePath, destPath);
        }
        else
        {
            File.Copy(filePath, destPath, true);
        }

        string editorResourceFolder = Path.Combine(
            settingsService.ModRootFolderPath,
            "interface",
            "editor"
        );
        _ = Directory.CreateDirectory(editorResourceFolder);

        string iconName = $"GFX_{Path.GetFileNameWithoutExtension(filePath)}";
        WriteFocusIconRegistrationFile(editorResourceFolder, iconName, relativePath);
        WriteFocusIconShineFile(editorResourceFolder, iconName, relativePath);

        return new RegisterFocusIconResult(iconName, destPath, isNeedConvertToDds);
    }

    private static void WriteFocusIconShineFile(
        string editorResourceFolder,
        string name,
        string relativePath
    )
    {
        string focusIconShineRegistrationFilePath = Path.Combine(
            editorResourceFolder,
            "focusIconShine.gfx"
        );
        Node spriteTypesNode;
        if (
            File.Exists(focusIconShineRegistrationFilePath)
            && TextParser.TryParse(focusIconShineRegistrationFilePath, out var rootNode, out _)
        )
        {
            spriteTypesNode = rootNode
                .Nodes.AsValueEnumerable()
                .First(static node => node.Key.EqualsIgnoreCase(SpriteTypesKey));
        }
        else
        {
            rootNode = Node.Create(string.Empty);
            spriteTypesNode = Node.Create(SpriteTypesKey);
            rootNode.AllArray = [spriteTypesNode];
        }
        spriteTypesNode.AddChild(CreateFocusShineNode(name, relativePath));
        File.WriteAllText(
            focusIconShineRegistrationFilePath,
            rootNode.ToScript(),
            App.Utf8EncodingWithoutBom
        );
    }

    private static void WriteFocusIconRegistrationFile(
        string editorResourceFolder,
        string name,
        string relativePath
    )
    {
        string focusIconRegistrationFilePath = Path.Combine(editorResourceFolder, "focusIcon.gfx");

        Node spriteTypesNode;
        var spriteType = CreateFocusSpriteTypeNode(name, relativePath);
        if (
            File.Exists(focusIconRegistrationFilePath)
            && TextParser.TryParse(focusIconRegistrationFilePath, out var rootNode, out _)
        )
        {
            spriteTypesNode = rootNode
                .Nodes.AsValueEnumerable()
                .First(static node => node.Key.EqualsIgnoreCase(SpriteTypesKey));
        }
        else
        {
            rootNode = Node.Create(string.Empty);
            spriteTypesNode = Node.Create(SpriteTypesKey);
            rootNode.AllArray = [spriteTypesNode];
        }
        spriteTypesNode.AddChild(spriteType);
        File.WriteAllText(focusIconRegistrationFilePath, rootNode.ToScript(), App.Utf8EncodingWithoutBom);
    }

    private static Node CreateFocusSpriteTypeNode(string name, string relativePath)
    {
        var spriteType = Node.Create(SpriteTypeKey);
        spriteType.AllArray =
        [
            ChildHelper.LeafQString("name", name),
            ChildHelper.LeafQString("texturefile", relativePath)
        ];
        return spriteType;
    }

    private static Node CreateFocusShineNode(string name, string relativePath)
    {
        var animationStartNode = Node.Create("animation");
        animationStartNode.AllArray =
        [
            ChildHelper.LeafQString("animationmaskfile", relativePath),
            // animated file
            ChildHelper.LeafQString(
                "animationtexturefile",
                "gfx/interface/goals/shine_overlay.dds"
            ),
            ChildHelper.Leaf("animationrotation", -90),
            ChildHelper.Leaf("animationlooping", false),
            ChildHelper.Leaf("animationtime", 0.75M),
            ChildHelper.Leaf("animationdelay", 0),
            ChildHelper.LeafQString("animationblendmode", "add"),
            ChildHelper.LeafQString("animationtype", "scrolling"),
            ChildHelper.Node(
                "animationrotationoffset",
                [ChildHelper.LeafString("x", "0.0"), ChildHelper.LeafString("y", "0.0")]
            ),
            ChildHelper.Node(
                "animationtexturescale",
                [ChildHelper.LeafString("x", "1.0"), ChildHelper.LeafString("y", "1.0")]
            )
        ];
        var animationEndNode = animationStartNode.Clone();
        animationEndNode
            .Leaves.First(static leaf => leaf.Key.EqualsIgnoreCase("animationrotation"))
            .Value = Types.Value.NewInt(90);

        var spriteTypeNode = Node.Create(SpriteTypeKey);
        var children = new List<Child>(6)
        {
            ChildHelper.LeafQString("name", name),
            ChildHelper.LeafQString("texturefile", relativePath),
            ChildHelper.LeafQString("effectFile", "gfx/FX/buttonstate.lua"),
            animationStartNode,
            animationEndNode,
            ChildHelper.Leaf("legacy_lazy_load", false)
        };
        spriteTypeNode.AllArray = children.ToArray();

        return spriteTypeNode;
    }

    private static void PngToDds(string pngFilePath, string outputFilePath)
    {
        Debug.Assert(Path.GetExtension(pngFilePath).EqualsIgnoreCase(".png"));

        using var image = Image.Load<Rgba32>(pngFilePath);

        var encoder = new BcEncoder
        {
            OutputOptions =
            {
                Quality = CompressionQuality.Balanced,
                Format = CompressionFormat.Bgra,
                FileFormat = OutputFileFormat.Dds
            }
        };

        using var fs = File.Open(outputFilePath, FileMode.Create);
        encoder.EncodeToStream(image, fs);
    }
}
