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
    public static async Task<string?> GetModNameAsync(string modRootDirectory)
    {
        string modFile = Path.Combine(modRootDirectory, GameConstants.ModDescriptorFileName);
        if (!File.Exists(modFile))
        {
            return null;
        }

        string content = await File.ReadAllTextAsync(modFile, App.Utf8EncodingWithoutBom).ConfigureAwait(false);

        if (!TextParser.TryParse(string.Empty, content, out var rootNode, out _))
        {
            return null;
        }

        return rootNode.LeavesValue.FirstOrDefault(leaf => leaf.Key.EqualsIgnoreCase("name"))?.ValueText;
    }
}
