using Avalonia.Collections;
using EnumsNET;
using Hoi4BlueprintBuilder.Core.Models;
using Hoi4BlueprintBuilder.Core.Services;

namespace Hoi4BlueprintBuilder.Core.ViewsModels;

[RegisterTransient<ProjectSettingsViewModel>]
public sealed class ProjectSettingsViewModel
{
    private readonly ProjectConfigService _projectConfigService;
    public IReadOnlyList<GameLanguage> GameLanguages => Enums.GetValues<GameLanguage>();
    public AvaloniaList<GameLanguage> SupportedLanguages { get; }

    public ProjectSettingsViewModel(ProjectConfigService projectConfigService)
    {
        _projectConfigService = projectConfigService;
        SupportedLanguages = [.. projectConfigService.SupportedLanguages];
    }

    public void OnUnload()
    {
        _projectConfigService.SupportedLanguages = SupportedLanguages.ToList();
    }
}
