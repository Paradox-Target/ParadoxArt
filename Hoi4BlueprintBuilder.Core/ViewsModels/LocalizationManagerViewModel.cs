using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DynamicData;
using DynamicData.Aggregation;
using Hoi4BlueprintBuilder.Core.Messages;
using Hoi4BlueprintBuilder.Core.Models;
using Hoi4BlueprintBuilder.Core.Models.Localization;
using Hoi4BlueprintBuilder.Core.Services;
using Hoi4BlueprintBuilder.Core.Services.GameResources.Localization;
using Hoi4BlueprintBuilder.Core.Views;
using Hoi4BlueprintBuilder.Localization.Strings;
using NLog;
using R3;
using ZLinq;

namespace Hoi4BlueprintBuilder.Core.ViewsModels;

[RegisterTransient<LocalizationManagerViewModel>]
public sealed partial class LocalizationManagerViewModel : ObservableObject, IClosed
{
    public ReadOnlyObservableCollection<LocalizationRow> LocalizationRows => _localizationRows;
    private readonly ReadOnlyObservableCollection<LocalizationRow> _localizationRows;

    public BindableReactiveProperty<string> SearchText { get; } = new(string.Empty);

    // 根据支持的语言动态构成的列模型（方便View绑定）
    public IReadOnlyList<GameLanguage> SupportedLanguages { get; }

    [ObservableProperty]
    public partial string LocalizationCount { get; private set; } = string.Empty;

    private readonly SourceList<LocalizationRow> _allRowsCache = new();
    private readonly IDisposable _disposable;
    private readonly LocalizationService _localizationService;
    private readonly NotificationService _notificationService;

    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public LocalizationManagerViewModel(
        LocalizationService localizationService,
        ProjectConfigService projectConfigService,
        NotificationService notificationService
    )
    {
        _localizationService = localizationService;
        _notificationService = notificationService;
        SupportedLanguages = projectConfigService.SupportedLanguages;

        LoadData();
        var predicate = SearchText
            .Debounce(TimeSpan.FromMilliseconds(250))
            .ObserveOnUIThreadDispatcher()
            .DistinctUntilChanged()
            .Select(Filter)
            .AsSystemObservable();
        var filteredConnection = _allRowsCache.Connect().RefCount().Filter(predicate);
        _disposable = Disposable.Combine(
            filteredConnection.Bind(out _localizationRows).Subscribe(),
            filteredConnection
                .Count()
                .Subscribe(count =>
                    LocalizationCount = string.Format(
                        LangResources.LocalizationManager_RowCount,
                        count.ToString()
                    )
                )
        );
        return;

        Func<LocalizationRow, bool> Filter(string text) =>
            row =>
            {
                return string.IsNullOrEmpty(text)
                    || row.Key.Contains(text)
                    || row.Languages.AsValueEnumerable().Any(lang => lang.Value.Contains(text));
            };
    }

    private void LoadData()
    {
        _allRowsCache.AddRange(_localizationService.GetAllModLocalisationRows());

        // 保证每行都有全部支持的语言占位符
        foreach (var row in _allRowsCache.Items)
        {
            foreach (var lang in SupportedLanguages)
            {
                if (row.Languages.AsValueEnumerable().All(l => l.Language != lang))
                {
                    row.Languages.Add(
                        new LocalizationLanguageItem(
                            lang,
                            row.Languages.Find(x => !string.IsNullOrWhiteSpace(x.FilePath))?.FilePath
                                ?? throw new ArgumentException("不能正确获取本地化文件的路径"),
                            string.Empty
                        )
                    );
                }
            }
        }
    }

    [RelayCommand]
    private void Save()
    {
        // 遍历所有有更改的内容，重新写入 _localizationService
        foreach (var row in _allRowsCache.Items)
        {
            foreach (var langItem in row.Languages.AsValueEnumerable().Where(item => item.IsChanged))
            {
                langItem.IsChanged = false;
                _localizationService.AddOrUpdateLocalisation(
                    langItem.FilePath,
                    langItem.Language,
                    row.Key,
                    langItem.Value
                );
            }
        }

        StrongReferenceMessenger.Default.Send(new SaveLocalizationMessage());
        _notificationService.Show(LangResources.SavedSuccessfully);
    }

    public void Close()
    {
        _disposable.Dispose();
        SearchText.Dispose();
        _allRowsCache.Dispose();
    }
}
