using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FluentAvalonia.UI.Controls;
using Hoi4BlueprintBuilder.Core.Messages;
using Hoi4BlueprintBuilder.Core.Models;
using Hoi4BlueprintBuilder.Core.Models.Focus;
using Hoi4BlueprintBuilder.Core.Services;
using Hoi4BlueprintBuilder.Core.Views;
using Hoi4BlueprintBuilder.Core.Views.Dialogs;
using Hoi4BlueprintBuilder.Core.ViewsModels.Dialogs;
using Hoi4BlueprintBuilder.Localization.Strings;
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
    private bool _isVisibleForTitleCommandBar;

    [ObservableProperty]
    private bool _isFocusTreeEditorAtCurrent;

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
    private readonly NavigationService _navigationService;
    private readonly TelemetryService _telemetryService;

    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public TitleCommandBarViewModel(
        SettingsService settingsService,
        MessageBoxService messageBoxService,
        TabViewService tabViewService,
        UserStatusService userStatusService,
        NavigationService navigationService,
        TelemetryService telemetryService
    )
    {
        _settingsService = settingsService;
        _messageBoxService = messageBoxService;
        _tabViewService = tabViewService;
        _userStatusService = userStatusService;
        _navigationService = navigationService;
        _telemetryService = telemetryService;

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
    private async Task CreateNewFocusTreeFile()
    {
        string focusTreeDirectory = Path.Combine(
            _settingsService.ModRootFolderPath,
            Keywords.Common,
            "national_focus"
        );
        var content = new CreateNewFocusTreeFileView();
        var viewModel = new CreateNewFocusTreeFileViewModel(focusTreeDirectory);
        content.DataContext = viewModel;

        var dialog = new ContentDialog
        {
            Title = LangResources.CreateNewFocusTreeFileView_Title,
            Content = content,
            PrimaryButtonText = "创建",
            CloseButtonText = "取消",
            IsPrimaryButtonEnabled = false
        };
        viewModel.PrimaryEnableChanged += enabled => dialog.IsPrimaryButtonEnabled = enabled;

        var result = await dialog.ShowAsync();
        viewModel.Clean();

        if (result != ContentDialogResult.Primary)
        {
            return;
        }

        Directory.CreateDirectory(focusTreeDirectory);

        string filePath = viewModel.FinalFilePath;
        if (File.Exists(filePath))
        {
            await _messageBoxService.ShowAsync("文件已存在, 无法创建同名文件.", "创建失败", MessageBoxIcon.Error);
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
            await _messageBoxService.ShowAsync($"创建国策树文件失败: {e.Message}", "创建失败", MessageBoxIcon.Error);
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
}
