using System.Text;
using Avalonia.Platform;

namespace Hoi4BlueprintBuilder.Core.Helpers;

public static class AssetLoadHelper
{
    public static readonly string AssetsFolder =
        $"avares://{typeof(AssetLoadHelper).Assembly.GetName().Name}/Assets";

    public static string GetContentText(string path)
    {
        using var stream = AssetLoader.Open(new Uri($"{AssetsFolder}/{path}"));
        using var reader = new StreamReader(stream, Encoding.UTF8);
        string text = reader.ReadToEnd();
        return text;
    }
}
