using Avalonia.Collections;
using ByteSizeLib;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EnumsNET;
using Hoi4BlueprintBuilder.Core.Extensions;
using Hoi4BlueprintBuilder.Core.Models;
using Hoi4BlueprintBuilder.Core.Services;
using Hoi4BlueprintBuilder.Core.Services.GameResources.Base;
using ZLinq;

namespace Hoi4BlueprintBuilder.Core.ViewsModels;

[RegisterTransient<ProjectSettingsViewModel>]
public sealed partial class ProjectSettingsViewModel : ObservableObject
{
    public IReadOnlyList<GameLanguage> GameLanguages => Enums.GetValues<GameLanguage>();
    public AvaloniaList<GameLanguage> SupportedLanguages { get; }

    [ObservableProperty]
    private string _gameLocalizationFilesInfo = "未计算";

    private readonly ProjectConfigService _projectConfigService;
    private readonly GameResourcesPathService _gameResourcesPathService;

    public ProjectSettingsViewModel(
        ProjectConfigService projectConfigService,
        GameResourcesPathService gameResourcesPathService
    )
    {
        _projectConfigService = projectConfigService;
        _gameResourcesPathService = gameResourcesPathService;
        SupportedLanguages = [.. projectConfigService.SupportedLanguages];
    }

    public void OnUnload()
    {
        _projectConfigService.SupportedLanguages = SupportedLanguages.ToList();
    }

    [RelayCommand]
    private async Task ComputeFilesSum()
    {
        long filesByteSum = 0;
        int filesCount = 0;

        await Task.Run(() =>
        {
            foreach (var language in SupportedLanguages)
            {
                string path = Path.Combine("localisation", language.ToGameLocalizationLanguage());
                var files = _gameResourcesPathService.GetAllFilePriorModByRelativePathForFolder(
                    path,
                    WatcherFilter.LocalizationFiles.Name,
                    SearchOption.AllDirectories
                );

                filesByteSum += files.AsValueEnumerable().Sum(filePath => new FileInfo(filePath).Length);
                filesCount += files.Count;
            }
        });

        GameLocalizationFilesInfo =
            $"文件数量: {filesCount}, 总大小: {ByteSize.FromBytes(filesByteSum).MebiBytes:F1} MB";
    }
}
