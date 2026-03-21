using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Hoi4BlueprintBuilder.Core.Models;
using Hoi4BlueprintBuilder.Core.Models.Focus;
using Hoi4BlueprintBuilder.Core.Services;
using Hoi4BlueprintBuilder.Core.Services.GameResources;
using Hoi4BlueprintBuilder.Core.Services.GameResources.Localization;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Hoi4BlueprintBuilder.Core.ViewsModels;

public sealed partial class FocusInfoViewModel : ObservableObject, IDisposable
{
    [LocalizationRequired]
    public string LocationSystemText => FocusNode.RelativePosition is null ? "绝对位置" : "相对位置";
    public bool IsRelativePositionVisible => FocusNode.RelativePosition is not null;

    public string RelativePositionFocusName =>
        $"{GetRelativePositionFocusLocalizedName()} ({FocusNode.RelativePosition?.Id ?? string.Empty})";

    private string GetRelativePositionFocusLocalizedName() =>
        FocusNode.RelativePosition is null
            ? string.Empty
            : LocalizationFormatService.GetFormatText(FocusNode.RelativePosition.Id);

    public decimal Cost
    {
        get => FocusNode.Cost;
        set
        {
            FocusNode.Cost = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(FocusDaysTip));
        }
    }

    public int X
    {
        get => FocusNode.X;
        set
        {
            _isSkipNotify = true;
            FocusNode.SetRawX(value);
            OnPropertyChanged();
            _isSkipNotify = false;
        }
    }

    public int Y
    {
        get => FocusNode.Y;
        set
        {
            _isSkipNotify = true;
            FocusNode.SetRawY(value);
            OnPropertyChanged();
            _isSkipNotify = false;
        }
    }

    public IReadOnlyList<GameLanguage> Languages { get; }

    [ObservableProperty]
    private string _idText;

    [ObservableProperty]
    private string _descriptionText;

    [ObservableProperty]
    private int _selectedLanguageIndex;
    private bool _isSkipNotify;
    private string FocusDescriptionKey => $"{FocusNode.Id}_desc";

    private static int _lastSelectedLanguageIndex;
    private static readonly SpriteService SpriteService =
        App.Current.Services.GetRequiredService<SpriteService>();
    private static readonly DefinesService DefinesService =
        App.Current.Services.GetRequiredService<DefinesService>();
    private const string DefineName = "NDefines.NFocus.FOCUS_POINT_DAYS";
    private static readonly LocalizationService LocalizationService =
        App.Current.Services.GetRequiredService<LocalizationService>();
    private static readonly LocalizationFormatService LocalizationFormatService =
        App.Current.Services.GetRequiredService<LocalizationFormatService>();

    public FocusInfoViewModel(FocusNode focusNode)
    {
        FocusNode = focusNode;

        _idText = LocalizationFormatService.GetFormatText(FocusNode.Id);
        _descriptionText = LocalizationFormatService.GetFormatText(FocusDescriptionKey);
        _selectedLanguageIndex = _lastSelectedLanguageIndex;

        FocusNode.PropertyChanged += FocusNodeOnPropertyChanged;
        Languages = App.Current.Services.GetRequiredService<ProjectConfigService>().SupportedLanguages;
    }

    private void FocusNodeOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // 避免重复通知
        if (_isSkipNotify)
        {
            return;
        }

        if (e.PropertyName == nameof(FocusNode.RawPosition))
        {
            OnPropertyChanged(nameof(X));
            OnPropertyChanged(nameof(Y));
        }
        else if (e.PropertyName == nameof(FocusNode.RelativePosition))
        {
            OnPropertyChanged(nameof(LocationSystemText));
            OnPropertyChanged(nameof(IsRelativePositionVisible));
            OnPropertyChanged(nameof(RelativePositionFocusName));
        }
    }

    public FocusNode FocusNode { get; }

    public string IconPath =>
        SpriteService.TryGetSpriteFilePath(FocusNode.Icon, out string? path) ? path : string.Empty;

    public string FocusDaysTip => GetFocusDaysTip();

    private string GetFocusDaysTip()
    {
        long focusCost = DefinesService.GetLong(DefineName);
        int totalDays = (int)(FocusNode.Cost * focusCost);
        return $" x {focusCost} = {totalDays} 天";
    }

    partial void OnIdTextChanged(string value)
    {
        if (_isUpdatingLanguage)
        {
            return;
        }

        LocalizationService.AddOrUpdateLocalisation(
            FocusNode.Path,
            Languages[SelectedLanguageIndex],
            FocusNode.Id,
            value
        );

        FocusNode.RefreshLocalizedName();
    }

    partial void OnDescriptionTextChanged(string value)
    {
        if (string.IsNullOrEmpty(value) || _isUpdatingLanguage)
        {
            return;
        }

        LocalizationService.AddOrUpdateLocalisation(
            FocusNode.Path,
            Languages[SelectedLanguageIndex],
            FocusDescriptionKey,
            value
        );
    }

    private bool _isUpdatingLanguage;

    partial void OnSelectedLanguageIndexChanged(int value)
    {
        _lastSelectedLanguageIndex = value;
        _isUpdatingLanguage = true;
        DescriptionText = LocalizationFormatService.GetFormatText(FocusDescriptionKey, Languages[value]);
        IdText = LocalizationFormatService.GetFormatText(FocusNode.Id, Languages[value]);
        _isUpdatingLanguage = false;
    }

    public void Dispose()
    {
        FocusNode.PropertyChanged -= FocusNodeOnPropertyChanged;
    }
}
