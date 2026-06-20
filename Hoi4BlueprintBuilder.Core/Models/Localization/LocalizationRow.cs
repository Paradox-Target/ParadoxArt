using CommunityToolkit.Mvvm.ComponentModel;
using ZLinq;

namespace Hoi4BlueprintBuilder.Core.Models.Localization;

public sealed class LocalizationRow(string key)
{
    public string Key { get; } = key;

    public List<LocalizationLanguageItem> Languages { get; } = [];

    // 前端使用索引器访问不同语言的文本值，方便 View 绑定
    // ReSharper disable once UnusedMember.Global
    public string this[GameLanguage language]
    {
        get =>
            Languages.AsValueEnumerable().FirstOrDefault(l => l.Language == language)?.Value ?? string.Empty;
        set
        {
            var item = Languages.AsValueEnumerable().FirstOrDefault(l => l.Language == language);
            if (item is not null)
            {
                item.Value = value;
            }
            else
            {
                Languages.Add(new LocalizationLanguageItem(language, string.Empty, value));
            }
        }
    }
}

public sealed partial class LocalizationLanguageItem(GameLanguage language, string filePath, string value)
    : ObservableObject
{
    public string FilePath { get; } = filePath;
    public GameLanguage Language { get; } = language;
    public bool IsChanged { get; set; }

    [ObservableProperty]
    public partial string Value { get; set; } = value;

    partial void OnValueChanged(string value)
    {
        IsChanged = true;
    }
}
