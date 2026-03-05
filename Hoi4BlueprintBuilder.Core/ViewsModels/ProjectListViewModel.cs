using System.Text;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using AvaloniaEdit.Utils;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using Hoi4BlueprintBuilder.Core.Constants;
using Hoi4BlueprintBuilder.Core.Extensions;
using Hoi4BlueprintBuilder.Core.Models;
using Hoi4BlueprintBuilder.Core.Services;
using Hoi4BlueprintBuilder.Core.Views;
using Hoi4BlueprintBuilder.Core.Views.Dialogs;
using Hoi4BlueprintBuilder.Core.ViewsModels.Dialogs;
using Hoi4BlueprintBuilder.Localization.Strings;
using ParadoxPower.CSharpExtensions;
using ParadoxPower.Parser;
using ParadoxPower.Process;
using R3;
using ZLinq;

namespace Hoi4BlueprintBuilder.Core.ViewsModels;

[RegisterTransient<ProjectListViewModel>]
public sealed partial class ProjectListViewModel : ObservableObject
{
    [ObservableProperty]
    private IEnumerable<ProjectItem> _projects;

    public BindableReactiveProperty<string> SearchText { get; } = new(string.Empty);

    private readonly SettingsService _settingsService;
    private readonly FileService _fileService;
    private readonly MessageBoxService _messageBoxService;
    private readonly NavigationService _navigationService;
    private readonly TelemetryService _telemetryService;
    private readonly IDisposable _disposable;

    public ProjectListViewModel(
        SettingsService settingsService,
        FileService fileService,
        MessageBoxService messageBoxService,
        NavigationService navigationService,
        TelemetryService telemetryService
    )
    {
        _settingsService = settingsService;
        _fileService = fileService;
        _messageBoxService = messageBoxService;
        _navigationService = navigationService;
        _telemetryService = telemetryService;
        Projects = _settingsService.Projects;

        _disposable = SearchText
            .Debounce(TimeSpan.FromMilliseconds(250))
            .ObserveOnUIThreadDispatcher()
            .Subscribe(
                settingsService,
                (text, settings) =>
                {
                    Projects = string.IsNullOrWhiteSpace(text)
                        ? settings.Projects
                        : settings.Projects.Where(project =>
                            project.Name.Contains(text, StringComparison.OrdinalIgnoreCase)
                        );
                }
            );
    }

    public void Cleanup()
    {
        _disposable.Dispose();
        SearchText.Dispose();
    }

    [RelayCommand]
    private async Task CreateNewProject()
    {
        var dialog = new ContentDialog
        {
            Title = LangResources.CreateNewProject_Title,
            PrimaryButtonText = LangResources.Common_Ok,
            CloseButtonText = LangResources.Common_Cancel,
            DefaultButton = ContentDialogButton.Primary,
            IsPrimaryButtonEnabled = false
        };
        var viewModel = new CreateNewProjectViewModel(enable => dialog.IsPrimaryButtonEnabled = enable);

        var view = new CreateNewProjectView { DataContext = viewModel };
        dialog.Content = view;
        if (await dialog.ShowAsync() != ContentDialogResult.Primary)
        {
            return;
        }

        await CreateProjectAsync(viewModel).ConfigureAwait(false);
        _telemetryService.TrackEvent("Project_Created");
    }

    private async Task CreateProjectAsync(CreateNewProjectViewModel viewModel)
    {
        Directory.CreateDirectory(viewModel.FinalFolder);
        _settingsService.Projects.Insert(0, new ProjectItem(viewModel.ModName, viewModel.FinalFolder));

        var root = new Node(string.Empty)
        {
            AllArray =
            [
                ChildHelper.LeafQString("name", viewModel.ModName),
                ChildHelper.LeafQString("path", viewModel.FinalFolder.Replace('\\', '/')),
                ChildHelper.LeafQString("supported_version", viewModel.SupportedVersion),
                ChildHelper.Node(
                    "tags",
                    [.. viewModel.Tags.Select(tag => LeafValue.Create(Types.Value.NewQString(tag)))]
                )
            ]
        };
        string script = root.ToScript();
        await File.WriteAllTextAsync(
            Path.Combine(viewModel.FinalFolder, GameConstants.ModDescriptorFileName),
            script
        );
        string? parentFolder = Path.GetDirectoryName(viewModel.FinalFolder);

        if (parentFolder is not null)
        {
            await File.WriteAllTextAsync(Path.Combine(parentFolder, $"{viewModel.FolderName}.mod"), script);
        }
        NavigateToMainView(viewModel.FinalFolder);
    }

    private void NavigateToMainView(string modRootFolderPath)
    {
        _settingsService.ModRootFolderPath = modRootFolderPath;
        _navigationService.NavigateTo<MainView>();
    }

    [RelayCommand]
    private async Task OpenProject()
    {
        var storageFolder = await _fileService.OpenFolderAsync();
        if (storageFolder is null)
        {
            return;
        }

        string? modName = await GetModNameAsync(storageFolder);

        if (modName is null)
        {
            await _messageBoxService.ShowErrorAsync(
                $"无法识别该文件夹为有效的模组文件夹，缺少 mod 描述文件({GameConstants.ModDescriptorFileName})或文件格式错误。",
                "打开项目失败"
            );
            return;
        }

        string modPath = storageFolder.TryGetLocalPath() ?? throw new ArgumentException();
        _settingsService.Projects.Insert(0, new ProjectItem(modName, modPath));

        NavigateToMainView(modPath);
    }

    private static async Task<string?> GetModNameAsync(IStorageFolder storageFolder)
    {
        var modStorageFile = await storageFolder.GetFileAsync(GameConstants.ModDescriptorFileName);
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

        return rootNode
            .Leaves.AsValueEnumerable()
            .FirstOrDefault(leaf => leaf.Key.EqualsIgnoreCase("name"))
            ?.ValueText;
    }

    [RelayCommand]
    private async Task OpenProjectItem(PointerPressedEventArgs e)
    {
        if (e.Source is not InputElement { DataContext: ProjectItem item })
        {
            return;
        }

        if (!item.IsPathExist)
        {
            var result = await _messageBoxService.ShowAsync(
                "项目不存在, 是否需要从列表中移除?",
                "项目不存在",
                MessageBoxIcon.Info,
                MessageBoxButtons.YesNo
            );
            if (result == MessageBoxResult.Yes)
            {
                _settingsService.Projects.Remove(item);
            }
            return;
        }

        int index = _settingsService.Projects.IndexOf(item);
        _settingsService.Projects.Move(index, 0);
        NavigateToMainView(item.DirectoryPath);
    }
}
