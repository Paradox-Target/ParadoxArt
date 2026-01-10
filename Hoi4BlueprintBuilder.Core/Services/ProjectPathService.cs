namespace Hoi4BlueprintBuilder.Core.Services;

[RegisterSingleton<ProjectPathService>]
public sealed class ProjectPathService(SettingsService settingsService)
{
    public string GetRelativeModPath(string path)
    {
        if (string.IsNullOrEmpty(settingsService.ModRootFolderPath))
        {
            return path;
        }

        return Path.GetRelativePath(settingsService.ModRootFolderPath, path);
    }

    public string GetAbsoluteModPath(string path)
    {
        if (string.IsNullOrEmpty(settingsService.ModRootFolderPath))
        {
            return path;
        }

        return Path.GetFullPath(path, settingsService.ModRootFolderPath);
    }
}
