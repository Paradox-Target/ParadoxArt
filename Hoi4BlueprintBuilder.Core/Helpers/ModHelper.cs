using System.Text;
using Avalonia.Platform.Storage;
using AvaloniaEdit.Utils;
using Hoi4BlueprintBuilder.Core.Constants;
using Hoi4BlueprintBuilder.Core.Extensions;
using ParadoxPower.CSharpExtensions;
using ParadoxPower.ZLinq;
using ZLinq;

namespace Hoi4BlueprintBuilder.Core.Helpers;

public static class ModHelper
{
    public static async Task<string?> GetModNameAsync(IStorageFolder modRootDirectory)
    {
        var modStorageFile = await modRootDirectory.GetFileAsync(GameConstants.ModDescriptorFileName);
        if (modStorageFile is null)
        {
            return null;
        }

        await using var reader = await modStorageFile.OpenReadAsync();
        string? content = FileReader.ReadFileContent(reader, Encoding.UTF8);
        if (content is null)
        {
            return null;
        }

        if (!TextParser.TryParse(string.Empty, content, out var rootNode, out _))
        {
            return null;
        }

        return rootNode.LeavesValue.FirstOrDefault(leaf => leaf.Key.EqualsIgnoreCase("name"))?.ValueText;
    }
}
