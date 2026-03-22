using System.Text;
using Avalonia.Platform;

namespace Hoi4BlueprintBuilder.Core.Helpers;

public static class AssetLoadHelper
{
    private const string BasePath = "avares://ParadoxArt.Core/Assets/";

    public static string GetContentText(string path)
    {
        using var stream = AssetLoader.Open(new Uri($"{BasePath}/{path}"));
        using var reader = new StreamReader(stream, Encoding.UTF8);
        string text = reader.ReadToEnd();
        return text;
    }
}
