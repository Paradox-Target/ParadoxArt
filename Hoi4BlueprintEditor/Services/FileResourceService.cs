using System.Diagnostics;
using System.IO;
using Hoi4BlueprintEditor.Extensions;
using Hoi4BlueprintEditor.Helpers;
using ParadoxPower.CSharpExtensions;
using ParadoxPower.Parser;
using ParadoxPower.Process;
using ZLinq;

namespace Hoi4BlueprintEditor.Services;

[RegisterSingleton<FileResourceService>]
public sealed class FileResourceService(SettingsService settingsService)
{
    private const string SpriteTypeKey = "SpriteType";
    private const string SpriteTypesKey = "spriteTypes";

    public (string Name, string DestFilePath) RegisterFocusIcon(string filePath)
    {
        Debug.Assert(ImageHelper.IsValidFocusImageFormat(filePath));

        string editorResourceFolder = Path.Combine(settingsService.ModRootFolderPath, "interface", "editor");
        _ = Directory.CreateDirectory(editorResourceFolder);

        // 注册文件中的Icon相对路径
        string relativePath = Path.Combine(
            "gfx",
            "editor",
            "interface",
            "focusIcon",
            Path.GetFileName(filePath)
        );
        string iconName = $"GFX_{Path.GetFileNameWithoutExtension(filePath)}";
        WriteFocusIconRegistrationFile(editorResourceFolder, iconName, relativePath);
        WriteFocusIconShineFile(editorResourceFolder, iconName, relativePath);

        // 复制图片到 mod 文件夹中
        string destPath = Path.Combine(settingsService.ModRootFolderPath, relativePath);
        _ = Directory.CreateDirectory(Path.GetDirectoryName(destPath) ?? string.Empty);
        File.Copy(filePath, destPath, true);

        return (iconName, destPath);
    }

    private static void WriteFocusIconShineFile(string editorResourceFolder, string name, string relativePath)
    {
        string focusIconShineRegistrationFilePath = Path.Combine(editorResourceFolder, "focusIconShine.gfx");
        Node spriteTypesNode;
        if (
            File.Exists(focusIconShineRegistrationFilePath)
            && TextParser.TryParse(focusIconShineRegistrationFilePath, out var rootNode, out _)
        )
        {
            spriteTypesNode = rootNode
                .Nodes.AsValueEnumerable()
                .First(node => node.Key.EqualsIgnoreCase(SpriteTypesKey));
        }
        else
        {
            rootNode = Node.Create(string.Empty);
            spriteTypesNode = Node.Create(SpriteTypesKey);
            rootNode.AllArray = [spriteTypesNode];
        }
        spriteTypesNode.AddChild(CreateFocusShineNode(name, relativePath));
        File.WriteAllText(focusIconShineRegistrationFilePath, rootNode.ToScript(), App.Utf8Encoding);
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
                .First(node => node.Key.EqualsIgnoreCase(SpriteTypesKey));
        }
        else
        {
            rootNode = Node.Create(string.Empty);
            spriteTypesNode = Node.Create(SpriteTypesKey);
            rootNode.AllArray = [spriteTypesNode];
        }
        spriteTypesNode.AddChild(spriteType);
        File.WriteAllText(focusIconRegistrationFilePath, rootNode.ToScript(), App.Utf8Encoding);
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
            ChildHelper.LeafQString("animationtexturefile", "gfx/interface/goals/shine_overlay.dds"),
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
        animationEndNode.Leaves.First(leaf => leaf.Key.EqualsIgnoreCase("animationrotation")).Value =
            Types.Value.NewInt(90);

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
}
