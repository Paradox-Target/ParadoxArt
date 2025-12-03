using System.ComponentModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using EnumsNET;
using Hoi4BlueprintEditor.Models;
using Hoi4BlueprintEditor.Models.Focus;
using Hoi4BlueprintEditor.Services;
using Hoi4BlueprintEditor.Services.GameResources;
using Hoi4BlueprintEditor.Services.GameResources.Localization;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Hoi4BlueprintEditor.ViewsModels;

public sealed partial class FocusInfoViewModel : ObservableObject, IDisposable
{
    [LocalizationRequired]
    public string LocationSystemText => FocusNode.RelativePosition is null ? "绝对位置" : "相对位置";
    public Visibility RelativePositionInfoVisibility =>
        FocusNode.RelativePosition is null ? Visibility.Collapsed : Visibility.Visible;

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

    public IReadOnlyList<GameLanguage> Languages { get; } = Enums.GetValues<GameLanguage>();

    [ObservableProperty]
    private string _idText;

    [ObservableProperty]
    private string _descriptionText;

    [ObservableProperty]
    private int _selectedLanguageIndex;
    private bool _isSkipNotify;

    public FocusInfoViewModel(FocusNode focusNode)
    {
        FocusNode = focusNode;

        _idText = LocalizationFormatService.GetFormatText(FocusNode.Id);
        _descriptionText = LocalizationFormatService.GetFormatText($"{FocusNode.Id}_desc");
        _selectedLanguageIndex = _lastSelectedLanguageIndex;

        FocusNode.PropertyChanged += FocusNodeOnPropertyChanged;
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
            OnPropertyChanged(nameof(RelativePositionInfoVisibility));
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

    private static int _lastSelectedLanguageIndex = (int)
        App.Current.Services.GetRequiredService<SettingsService>().GameLanguage;
    private static readonly SpriteService SpriteService =
        App.Current.Services.GetRequiredService<SpriteService>();
    private static readonly DefinesService DefinesService =
        App.Current.Services.GetRequiredService<DefinesService>();
    private const string DefineName = "NDefines.NFocus.FOCUS_POINT_DAYS";
    private static readonly LocalizationService LocalizationService =
        App.Current.Services.GetRequiredService<LocalizationService>();
    private static readonly LocalizationFormatService LocalizationFormatService =
        App.Current.Services.GetRequiredService<LocalizationFormatService>();

    partial void OnIdTextChanged(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return;
        }

        LocalizationService.AddOrUpdateLocalisation(
            FocusNode.Path,
            Languages[SelectedLanguageIndex],
            FocusNode.Id,
            value
        );
    }

    partial void OnDescriptionTextChanged(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return;
        }

        LocalizationService.AddOrUpdateLocalisation(
            FocusNode.Path,
            Languages[SelectedLanguageIndex],
            $"{FocusNode.Id}_desc",
            value
        );
    }

    partial void OnSelectedLanguageIndexChanged(int value)
    {
        _lastSelectedLanguageIndex = value;
    }

    public void Dispose()
    {
        FocusNode.PropertyChanged -= FocusNodeOnPropertyChanged;
    }
}
