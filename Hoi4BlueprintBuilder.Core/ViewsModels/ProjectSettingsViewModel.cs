using Avalonia.Collections;
using Avalonia.Platform.Storage;
using ByteSizeLib;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EnumsNET;
using Hoi4BlueprintBuilder.Core.Extensions;
using Hoi4BlueprintBuilder.Core.Helpers;
using Hoi4BlueprintBuilder.Core.Models;
using Hoi4BlueprintBuilder.Core.Services;
using Hoi4BlueprintBuilder.Core.Services.GameResources.Base;
using ZLinq;

namespace Hoi4BlueprintBuilder.Core.ViewsModels;

[RegisterTransient<ProjectSettingsViewModel>]
public sealed partial class ProjectSettingsViewModel : ObservableObject
{
    public AvaloniaList<string> DependenciesPath { get; }
    public IReadOnlyList<GameLanguage> GameLanguages => Enums.GetValues<GameLanguage>();
    public AvaloniaList<GameLanguage> SupportedLanguages { get; }

    [ObservableProperty]
    private string _gameLocalizationFilesInfo = "未计算";

    [ObservableProperty]
    private bool _isEnabledHasDepsToggleSwitch;

    private readonly ProjectConfigService _projectConfigService;
    private readonly GameResourcesPathService _gameResourcesPathService;
    private readonly FileService _fileService;

    public ProjectSettingsViewModel(
        ProjectConfigService projectConfigService,
        GameResourcesPathService gameResourcesPathService,
        FileService fileService
    )
    {
        _projectConfigService = projectConfigService;
        _gameResourcesPathService = gameResourcesPathService;
        _fileService = fileService;
        SupportedLanguages = [.. projectConfigService.SupportedLanguages];
        DependenciesPath = new AvaloniaList<string>(
            projectConfigService.Dependencies.Select(dep => dep.RootDirectory)
        );
        IsEnabledHasDepsToggleSwitch = DependenciesPath.Count > 0;
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
            $"本地化文件数量: {filesCount}, 总大小: {ByteSize.FromBytes(filesByteSum).MebiBytes:F1} MB";
    }

    [RelayCommand]
    private async Task PickDependencyModDirectory()
    {
        using var storageFolder = await _fileService.OpenFolderAsync("选择依赖模组的根目录");
        if (storageFolder is null)
        {
            return;
        }

        string rootDir = storageFolder.TryGetLocalPath() ?? throw new InvalidOperationException();
        if (DependenciesPath.AsValueEnumerable().Any(path => path.Equals(rootDir, PlatformHelper.Comparison)))
        {
            return;
        }

        string? modName = await ModHelper.GetModNameAsync(storageFolder);
        _projectConfigService.Dependencies.Add(new DependencyModInfo(modName ?? "Unknown", rootDir));
        DependenciesPath.Add(rootDir);
    }
}
