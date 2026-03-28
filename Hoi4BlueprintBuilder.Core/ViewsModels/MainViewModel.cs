using Hoi4BlueprintBuilder.Core.Services;

namespace Hoi4BlueprintBuilder.Core.ViewsModels;

[RegisterSingleton<MainViewModel>]
public sealed class MainViewModel
{
    public MainViewModel(
        SettingsService settingsService,
        ProjectConfigService projectConfigService,
        TelemetryService telemetryService,
        GameModDescriptorService descriptorService
    )
    {
        _ = Task.Run(() =>
        {
            if (settingsService.IsFirstRun)
            {
                return;
            }

            var properties = new Dictionary<string, string>
            {
                { "IsSubMod", (projectConfigService.Dependencies.Count > 0).ToString() },
                { "SupportedLanguages", string.Join(',', projectConfigService.SupportedLanguages) }
            };
            string modDirectory = settingsService.ModRootFolderPath;
            if (Directory.Exists(modDirectory))
            {
                int count = Directory.EnumerateFiles(modDirectory, "*", SearchOption.AllDirectories).Count();
                properties["Project_File_Count"] = count.ToString();
            }

            properties["Replace_Path_Count"] = descriptorService.ReplacePaths.Count.ToString();

            telemetryService.TrackEvent("User_Project_Info", properties);
        });
    }
}
