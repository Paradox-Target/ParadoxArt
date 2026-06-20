using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FluentAvalonia.UI.Controls;
using Hoi4BlueprintBuilder.Core.Helpers;
using Hoi4BlueprintBuilder.Core.Messages;
using Hoi4BlueprintBuilder.Core.Models;
using Hoi4BlueprintBuilder.Core.Models.Focus;
using Hoi4BlueprintBuilder.Core.Services;
using Hoi4BlueprintBuilder.Core.Views;
using Hoi4BlueprintBuilder.Core.Views.Dialogs;
using Hoi4BlueprintBuilder.Core.ViewsModels.Dialogs;
using Hoi4BlueprintBuilder.Localization.Strings;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using ParadoxPower.CSharpExtensions;
using ParadoxPower.Process;

namespace Hoi4BlueprintBuilder.Core.ViewsModels;

[RegisterSingleton<TitleCommandBarViewModel>]
public sealed partial class TitleCommandBarViewModel : ObservableObject
{
    /// <summary>
    /// 当前国策树中可独立切换的叶子条件列表
    /// </summary>
    public IReadOnlyList<ConditionItem> ConditionItems => GetConditionItems();

    [ObservableProperty]
    public partial bool IsVisibleForTitleCommandBar { get; set; }

    [ObservableProperty]
    public partial bool IsFocusTreeEditorAtCurrent { get; set; }

    private bool CanSave => _tabViewService.CurrentItem is ISave;

    /// <summary>
    /// 获取当前国策树中的可切换条件列表
    /// </summary>
    private IReadOnlyList<ConditionItem> GetConditionItems()
    {
        if (_tabViewService.CurrentItem is not FocusTreeEditorView focusTreeEditorView)
        {
            return [];
        }

        return focusTreeEditorView.ViewModel.ConditionItems;
    }

    private readonly SettingsService _settingsService;
    private readonly MessageBoxService _messageBoxService;
    private readonly TabViewService _tabViewService;
    private readonly UserStatusService _userStatusService;
    private readonly TelemetryService _telemetryService;
    private readonly FileResourceService _fileResourceService;

    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public TitleCommandBarViewModel(
        SettingsService settingsService,
        MessageBoxService messageBoxService,
        TabViewService tabViewService,
        UserStatusService userStatusService,
        NavigationService navigationService,
        TelemetryService telemetryService,
        FileResourceService fileResourceService
    )
    {
        _settingsService = settingsService;
        _messageBoxService = messageBoxService;
        _tabViewService = tabViewService;
        _userStatusService = userStatusService;
        _telemetryService = telemetryService;
        _fileResourceService = fileResourceService;

        _tabViewService.CurrentItemChanged += currentItem =>
        {
            if (IsVisibleForTitleCommandBar)
            {
                OnPropertyChanged(nameof(ConditionItems));
            }

            IsFocusTreeEditorAtCurrent = currentItem is FocusTreeEditorView;
            SaveFileCommand.NotifyCanExecuteChanged();
        };

        navigationService.ViewChanged += currentView =>
        {
            IsVisibleForTitleCommandBar = currentView is MainView;
        };
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private void SaveFile()
    {
        if (_tabViewService.CurrentItem is not ISave save)
        {
            return;
        }

        save.Save();
    }

    [RelayCommand]
    private void ExportFocusTreeScreenshot()
    {
        StrongReferenceMessenger.Default.Send(new SaveFocusTreeToPngMessage());
        _telemetryService.TrackEvent("ExportFocusTreeScreenshot");
    }

    [RelayCommand]
    public async Task CreateNewFocusTreeFileAsync(string? fileName)
    {
        string focusTreeDirectory = Path.Combine(
            _settingsService.ModRootFolderPath,
            Keywords.Common,
            "national_focus"
        );
        var content = new CreateNewFocusTreeFileView();
        var viewModel = new CreateNewFocusTreeFileViewModel(focusTreeDirectory)
        {
            FileName = fileName ?? string.Empty
        };
        content.DataContext = viewModel;

        var dialog = new FAContentDialog
        {
            Title = LangResources.CreateNewFocusTreeFileView_Title,
            Content = content,
            PrimaryButtonText = "创建",
            CloseButtonText = LangResources.Common_Cancel,
            IsPrimaryButtonEnabled = false
        };
        viewModel.PrimaryEnableChanged += enabled => dialog.IsPrimaryButtonEnabled = enabled;

        var result = await dialog.ShowAsync();
        viewModel.Clean();

        if (result != FAContentDialogResult.Primary)
        {
            return;
        }

        Directory.CreateDirectory(focusTreeDirectory);

        string filePath = viewModel.FinalFilePath;
        if (File.Exists(filePath))
        {
            await _messageBoxService.ShowErrorAsync(
                LangResources.TitleCommandBar_FileExistsCannotCreate,
                LangResources.TitleCommandBar_CreateFailed
            );
            return;
        }

        try
        {
            await File.WriteAllTextAsync(
                filePath,
                CreateNewFocusTree(viewModel.Id, viewModel.CountryTag, viewModel.IsDefaultFocusTree),
                App.Utf8EncodingWithoutBom
            );

            _userStatusService.CurrentSelectedFile = SystemFileItem.FromFilePath(filePath);
            _tabViewService.AddTabFromIoc<FocusTreeEditorView>(filePath);
        }
        catch (Exception e)
        {
            Log.Error(e, "创建国策树文件失败");
            await _messageBoxService.ShowErrorAsync(
                string.Format(LangResources.TitleCommandBar_CreateFocusTreeMessageFailed, e.Message),
                LangResources.TitleCommandBar_CreateFailed
            );
        }
    }

    private static string CreateNewFocusTree(string id, string countryTag, bool isDefaultFocusTree)
    {
        var rootNode = new Node(string.Empty);
        var focusTreeNode = new Node("focus_tree");
        var countryNode = new Node("country");
        if (isDefaultFocusTree)
        {
            countryNode.AllArray = [ChildHelper.Leaf("factor", 1)];
        }
        else
        {
            countryNode.AllArray =
            [
                ChildHelper.Leaf("factor", 0),
                ChildHelper.Node(
                    "modifier",
                    [ChildHelper.Leaf("add", 100), ChildHelper.LeafString("tag", countryTag)]
                )
            ];
        }
        focusTreeNode.AllArray =
        [
            ChildHelper.LeafString("id", id),
            countryNode,
            ChildHelper.Leaf("default", isDefaultFocusTree)
        ];

        rootNode.AllArray = [focusTreeNode];
        return rootNode.ToScript();
    }

    [RelayCommand]
    private void OpenLocalizationManager()
    {
        _tabViewService.AddSingleTabFromIoc<LocalizationManagerView>();
        _telemetryService.TrackEvent("OpenLocalizationManager");
    }

    [RelayCommand]
    private async Task OpenImageImport()
    {
        var fileService = App.Current.Services.GetRequiredService<FileService>();
        _telemetryService.TrackEvent("ImageImport");

        var type = new FilePickerFileType("Image") { Patterns = ["*.png", "*.dds"] };
        var imageFile = await fileService.OpenFileAsync(
            new FilePickerOpenOptions
            {
                Title = "选择图片",
                AllowMultiple = false,
                FileTypeFilter = [type]
            }
        );
        if (imageFile is null)
        {
            return;
        }

        string path = imageFile.TryGetLocalPath() ?? throw new PlatformNotSupportedException();
        if (!ImageHelper.IsValidFocusImageFormat(path))
        {
            _ = _messageBoxService.ShowErrorAsync(
                LangResources.TitleCommandBar_UnsupportedImageFormat,
                LangResources.TitleCommandBar_ImportImageFailed
            );
            return;
        }

        var result = _fileResourceService.RegisterFocusIcon(path);
        _ = _messageBoxService.ShowAsync(
            string.Format(LangResources.TitleCommandBar_ImportSuccess, result.DestFilePath, result.SpriteName)
        );
    }
}
