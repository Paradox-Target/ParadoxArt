using System.Text;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using Hoi4BlueprintBuilder.Core.Constants;
using Hoi4BlueprintBuilder.Core.Helpers;
using Hoi4BlueprintBuilder.Core.Models;
using Hoi4BlueprintBuilder.Core.Services;
using Hoi4BlueprintBuilder.Core.Views;
using Hoi4BlueprintBuilder.Core.Views.Dialogs;
using Hoi4BlueprintBuilder.Core.ViewsModels.Dialogs;
using Hoi4BlueprintBuilder.Localization.Strings;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using ParadoxPower.CSharpExtensions;
using ParadoxPower.Parser;
using ParadoxPower.Process;
using R3;

namespace Hoi4BlueprintBuilder.Core.ViewsModels;

[RegisterTransient<ProjectListViewModel>]
public sealed partial class ProjectListViewModel : ObservableObject
{
    [ObservableProperty]
    public partial IEnumerable<ProjectItem> Projects { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CopyProjectPathCommand), nameof(OpenProjectFolderInExplorerCommand))]
    public partial ProjectItem? RightClickedItem { get; set; }
    public BindableReactiveProperty<string> SearchText { get; } = new(string.Empty);

    private readonly SettingsService _settingsService;
    private readonly FileService _fileService;
    private readonly MessageBoxService _messageBoxService;
    private readonly NavigationService _navigationService;
    private readonly TelemetryService _telemetryService;
    private readonly ClipboardService _clipboardService;
    private readonly NotificationService _notificationService;
    private readonly IDisposable _disposable;
    private bool IsValidRightClickedItem => RightClickedItem is not null && RightClickedItem.IsPathExist;

    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public ProjectListViewModel(
        SettingsService settingsService,
        FileService fileService,
        MessageBoxService messageBoxService,
        NavigationService navigationService,
        TelemetryService telemetryService,
        ClipboardService clipboardService,
        NotificationService notificationService
    )
    {
        _settingsService = settingsService;
        _fileService = fileService;
        _messageBoxService = messageBoxService;
        _navigationService = navigationService;
        _telemetryService = telemetryService;
        _clipboardService = clipboardService;
        _notificationService = notificationService;
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
        var dialog = new FAContentDialog
        {
            Title = LangResources.CreateNewProject_Title,
            PrimaryButtonText = LangResources.Common_Ok,
            CloseButtonText = LangResources.Common_Cancel,
            DefaultButton = FAContentDialogButton.Primary,
            IsPrimaryButtonEnabled = false
        };
        var viewModel = new CreateNewProjectViewModel(
            _settingsService,
            enable => dialog.IsPrimaryButtonEnabled = enable
        );

        var view = new CreateNewProjectView { DataContext = viewModel };
        dialog.Content = view;
        if (await dialog.ShowAsync() != FAContentDialogResult.Primary)
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
            script,
            Encoding.UTF8
        );
        string? parentFolder = Path.GetDirectoryName(viewModel.FinalFolder);

        if (parentFolder is not null)
        {
            await File.WriteAllTextAsync(
                Path.Combine(parentFolder, $"{viewModel.FolderName}.mod"),
                script,
                Encoding.UTF8
            );
        }

        NavigateToMainView(
            viewModel.FinalFolder,
            () =>
            {
                App.Current.Services.GetRequiredService<ProjectConfigService>().SupportedLanguages =
                    viewModel.SupportedLanguages.ToList();
            }
        );
    }

    [RelayCommand]
    private async Task OpenProject()
    {
        using var storageFolder = await _fileService.OpenFolderAsync(
            LangResources.ProjectList_SelectModRootFolder
        );
        if (storageFolder is null)
        {
            return;
        }

        string? modName = await ModHelper.GetModNameAsync(storageFolder);

        if (modName is null)
        {
            await _messageBoxService.ShowErrorAsync(
                string.Format(
                    LangResources.ProjectList_InvalidModFolder,
                    GameConstants.ModDescriptorFileName
                ),
                LangResources.ProjectList_OpenProjectFailed
            );
            return;
        }

        string modPath = storageFolder.TryGetLocalPath() ?? throw new ArgumentException();
        _settingsService.Projects.Insert(0, new ProjectItem(modName, modPath));

        NavigateToMainView(
            modPath,
            () =>
            {
                var service = App.Current.Services.GetRequiredService<GameModDescriptorService>();
                if (service.DependenciesName.Length != 0)
                {
                    _messageBoxService.ShowAsync(
                        LangResources.ProjectList_SubmodDependencyDetected,
                        LangResources.Common_Prompt
                    );
                }
            }
        );
    }

    private void NavigateToMainView(string modRootFolderPath, Action? handle = null)
    {
        Log.Info("打开项目: {Path}", modRootFolderPath);
        _settingsService.ModRootFolderPath = modRootFolderPath;
        handle?.Invoke();
        _navigationService.NavigateTo<MainView>();
    }

    [RelayCommand]
    private void RemoveProjectItem()
    {
        if (RightClickedItem is null)
        {
            Log.Warn("尝试移除项目但 RightClickedItem 为空");
            return;
        }

        _settingsService.Projects.Remove(RightClickedItem);
    }

    [RelayCommand(CanExecute = nameof(IsValidRightClickedItem))]
    private async Task CopyProjectPath()
    {
        if (RightClickedItem is null)
        {
            Log.Warn("尝试复制项目路径但 RightClickedItem 为空");
            return;
        }

        if (!RightClickedItem.IsPathExist)
        {
            return;
        }

        await _clipboardService.SetTextAsync(RightClickedItem.DirectoryPath);
        _notificationService.Show(LangResources.CopiedToClipboard);
    }

    [RelayCommand(CanExecute = nameof(IsValidRightClickedItem))]
    private async Task OpenProjectFolderInExplorer()
    {
        if (RightClickedItem is null)
        {
            Log.Warn("尝试打开项目文件夹但 RightClickedItem 为空");
            return;
        }

        if (!RightClickedItem.IsPathExist)
        {
            return;
        }

        await _fileService.LaunchUriAsync(RightClickedItem.DirectoryPath).ConfigureAwait(false);
    }

    [RelayCommand]
    private async Task OpenProjectItem(PointerPressedEventArgs e)
    {
        if (e.Properties.PointerUpdateKind != PointerUpdateKind.LeftButtonPressed)
        {
            return;
        }

        if (e.Source is not InputElement { DataContext: ProjectItem item })
        {
            return;
        }

        if (!item.IsPathExist)
        {
            var result = await _messageBoxService.ShowAsync(
                LangResources.ProjectList_ProjectNotFoundRemovePrompt,
                LangResources.ProjectList_ProjectNotFound,
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
