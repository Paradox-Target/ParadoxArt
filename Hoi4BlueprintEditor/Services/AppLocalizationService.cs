using System.Globalization;
using System.Resources;
using Hoi4BlueprintEditor.Resources.Strings;

namespace Hoi4BlueprintEditor.Services;

public sealed class AppLocalizationService
{
    private readonly ResourceManager _resourceManager;

    public AppLocalizationService()
    {
        _resourceManager = new ResourceManager(typeof(Strings));
    }

    public string GetString(string key)
    {
        // ResourceManager.GetString -> Thread.CurrentThread.CurrentUICulture
        return _resourceManager.GetString(key) ?? $"[{key}]";
    }

    public string GetString(string key, CultureInfo culture)
    {
        // ResourceManager.GetString -> 目标culture
        return _resourceManager.GetString(key, culture) ?? $"[{key}]";
    }
}
