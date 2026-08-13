using System.Diagnostics.CodeAnalysis;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Hoi4BlueprintBuilder.Core.Extensions;
using Hoi4BlueprintBuilder.Core.Services.GameResources.Localization;
using NLog;
using ParadoxPower.Process;

namespace Hoi4BlueprintBuilder.Core.Services.GameResources.Modifiers;

[RegisterSingleton<ModifierService>]
public sealed class ModifierService
{
    private readonly LocalizationService _localizationService;
    private readonly ModiferLocalizationFormatService _modifierLocalizationFormatService;

    public ModifierService(
        LocalizationService localizationService,
        ModiferLocalizationFormatService modifierLocalizationFormatService
    )
    {
        _localizationService = localizationService;
        _modifierLocalizationFormatService = modifierLocalizationFormatService;
    }

    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private static readonly ImmutableSolidColorBrush Yellow = new(Color.FromRgb(255, 189, 0));

    public IBrush? GetModifierBrush(Leaf leafModifier, string modifierFormat)
    {
        if (!leafModifier.Value.TryGetDouble(out double value))
        {
            return null;
        }

        return GetModifierBrush(value, modifierFormat);
    }

    private static IBrush? GetModifierBrush(double value, string modifierFormat)
    {
        if (value == 0.0)
        {
            return Yellow;
        }

        var modifierType = GetModifierType(modifierFormat);
        if (modifierType == ModifierEffectType.Unknown)
        {
            return null;
        }

        if (value > 0.0)
        {
            if (modifierType == ModifierEffectType.Positive)
            {
                return Brushes.Green;
            }

            if (modifierType == ModifierEffectType.Negative)
            {
                return Brushes.Red;
            }
        }
        else
        {
            if (modifierType == ModifierEffectType.Positive)
            {
                return Brushes.Red;
            }

            if (modifierType == ModifierEffectType.Negative)
            {
                return Brushes.Green;
            }
        }

        return null;
    }

    public IBrush? GetTerrainModifierBrush(string stat, double value)
    {
        string format = GetTerrainModifierFormat(stat);
        return GetModifierBrush(value, format);
    }

    public bool TryGetLocalizationName(string modifierKey, [NotNullWhen(true)] out string? value)
    {
        if (_localizationService.TryGetValueInAll(modifierKey, out value))
        {
            return true;
        }

        if (_localizationService.TryGetValueInAll($"MODIFIER_{modifierKey}", out value))
        {
            return true;
        }

        if (_localizationService.TryGetValueInAll($"MODIFIER_NAVAL_{modifierKey}", out value))
        {
            return true;
        }

        if (_localizationService.TryGetValueInAll($"MODIFIER_UNIT_LEADER_{modifierKey}", out value))
        {
            return true;
        }

        if (_localizationService.TryGetValueInAll($"MODIFIER_ARMY_LEADER_{modifierKey}", out value))
        {
            return true;
        }

        return false;
    }

    public string GetLocalizationName(string modifierKey)
    {
        if (TryGetLocalizationName(modifierKey, out string? value))
        {
            return value;
        }

        return modifierKey;
    }

    public bool TryGetLocalizationFormat(string modifier, [NotNullWhen(true)] out string? result)
    {
        if (_modifierLocalizationFormatService.TryGetLocalizationFormat(modifier, out result))
        {
            return true;
        }

        if (
            _localizationService.TryGetValueInAll($"{modifier}_tt", out result)
            || _localizationService.TryGetValueInAll($"{modifier}_DIFF", out result)
        )
        {
            return true;
        }

        return _localizationService.TryGetValueInAll(modifier, out result);
    }

    private static ModifierEffectType GetModifierType(string modifierFormat)
    {
        for (int index = modifierFormat.Length - 1; index >= 0; index--)
        {
            char c = modifierFormat[index];
            switch (c)
            {
                case '+':
                    return ModifierEffectType.Positive;
                case '-':
                    return ModifierEffectType.Negative;
            }
        }

        return ModifierEffectType.Unknown;
    }

    /// <summary>
    /// 获取 Modifier 数值的显示值
    /// </summary>
    /// <param name="leafModifier">包含关键字和对应值的修饰符对象</param>
    /// <param name="modifierDisplayFormat">修饰符对应的格式化设置文本, 为空时使用百分比格式</param>
    /// <returns>应用<c>modifierDisplayFormat</c>格式的<c>LeafModifier.Value</c>的的显示值</returns>
    public string GetDisplayValue(Leaf leafModifier, string modifierDisplayFormat)
    {
        if (leafModifier.Value.TryGetDouble(out double value))
        {
            return GetDisplayValue(value, modifierDisplayFormat);
        }

        return leafModifier.ValueText;
    }

    public string GetTerrainModifierDisplayValue(string stat, double value)
    {
        return GetDisplayValue(value, GetTerrainModifierFormat(stat));
    }

    private string GetTerrainModifierFormat(string stat)
    {
        return _localizationService.TryGetValueInAll($"STAT_ADJUSTER_{stat}_DIFF", out string? format)
            ? format
            : string.Empty;
    }

    public string GetDisplayValue(double value, string modifierDisplayFormat, bool withPlusSign = true)
    {
        string sign = withPlusSign && value > 0.0 ? "+" : string.Empty;
        char displayDigits = GetDisplayDigits(modifierDisplayFormat);
        bool isPercentage =
            string.IsNullOrEmpty(modifierDisplayFormat) || modifierDisplayFormat.Contains('%');
        char format = isPercentage ? 'P' : 'F';

        return $"{sign}{value.ToString($"{format}{displayDigits}")}";
    }

    private static char GetDisplayDigits(string modifierDescription)
    {
        char displayDigits = '1';
        for (int i = modifierDescription.Length - 1; i >= 0; i--)
        {
            char c = modifierDescription[i];
            if (char.IsDigit(c))
            {
                displayDigits = c;
                break;
            }
        }

        return displayDigits;
    }
}

public enum ModifierEffectType : byte
{
    Unknown,
    Positive,
    Negative
}
