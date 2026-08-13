using Avalonia.Controls.Documents;
using Hoi4BlueprintBuilder.Core.Extensions;
using Hoi4BlueprintBuilder.Core.Services.GameResources.Localization;
using NLog;
using ParadoxPower.CSharpExtensions;
using ParadoxPower.Process;
using ParadoxPower.ZLinq;
using ZLinq;

namespace Hoi4BlueprintBuilder.Core.Services.GameResources.Modifiers;

[RegisterSingleton<ModifierDisplayService>]
public sealed class ModifierDisplayService
{
    private readonly LocalizationFormatService _localizationFormatService;
    private readonly LocalizationService _localizationService;
    private readonly ModifierService _modifierService;
    private readonly TerrainService _terrainService;
    private readonly LocalizationKeyMappingService _localisationKeyMappingService;

    private const string NodeModifierChildrenPrefix = "  ";
    private const string CustomEffectTooltipKey = "custom_effect_tooltip";
    private const string CustomModifierTooltipKey = "custom_modifier_tooltip";
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public ModifierDisplayService(
        LocalizationFormatService localisationFormatFormatService,
        ModifierService modifierService,
        LocalizationKeyMappingService localisationKeyMappingService,
        TerrainService terrainService,
        LocalizationService localizationService
    )
    {
        _localizationFormatService = localisationFormatFormatService;
        _modifierService = modifierService;
        _localisationKeyMappingService = localisationKeyMappingService;
        _terrainService = terrainService;
        _localizationService = localizationService;
    }

    public IReadOnlyCollection<Inline> GetDescription(IEnumerable<Child> modifiers)
    {
        var inlines = new List<Inline>(8);

        foreach (var child in modifiers)
        {
            IEnumerable<Inline>? addedInlines = null;
            if (child.TryGetLeaf(out var leaf))
            {
                if (IsCustomToolTip(leaf.Key))
                {
                    addedInlines = _localizationFormatService.GetFormatTextWithColor(leaf.ValueText);
                }
                else
                {
                    addedInlines = GetDescriptionForLeaf(leaf);
                }
            }
            else if (child.TryGetNode(out var node))
            {
                addedInlines = GetModifierDescriptionForNode(node);
            }

            if (addedInlines is not null)
            {
                inlines.AddRange(addedInlines);
            }
            inlines.Add(new LineBreak());
        }

        RemoveLastLineBreak(inlines);
        return inlines;
    }

    private static bool IsCustomToolTip(string modifierKey)
    {
        return modifierKey.EqualsIgnoreCase(CustomEffectTooltipKey)
            || modifierKey.EqualsIgnoreCase(CustomModifierTooltipKey);
    }

    private List<Inline> GetDescriptionForLeaf(Leaf modifier)
    {
        var modifierKey = _localisationKeyMappingService.TryGetValue(modifier.Key, out var mappingKey)
            ? mappingKey
            : modifier.Key;
        var inlines = new List<Inline>(4);
        GetModifierColorTextFromText(modifierKey, inlines);
        if (inlines.Count != 0 && inlines[^1] is Run run)
        {
            string? text = run.Text?.TrimEnd();
            if (text is not null && !text.EndsWith(':') && !text.EndsWith('：'))
            {
                run.Text += ": ";
            }
        }

        if (modifier.Value.IsInt || modifier.Value.IsFloat)
        {
            string modifierFormat = _modifierService.TryGetLocalizationFormat(modifierKey, out string? result)
                ? result
                : string.Empty;
            inlines.Add(GetRun(modifier, modifierFormat));
        }
        else
        {
            inlines.Add(new Run { Text = modifier.ValueText });
        }
        return inlines;
    }

    private void GetModifierColorTextFromText(string modifierKey, List<Inline> inlines)
    {
        string modifierName = _modifierService.GetLocalizationName(modifierKey);
        inlines.AddRange(_localizationFormatService.GetFormatTextWithColorByText(modifierName));
    }

    private List<Inline> GetModifierDescriptionForNode(Node nodeModifier)
    {
        if (_terrainService.Contains(nodeModifier.Key))
        {
            return GetTerrainModifierDescription(nodeModifier);
        }

        return GetDescriptionForUnknownNode(nodeModifier);
    }

    /// <summary>
    /// 获取地形修饰符的描述
    /// </summary>
    /// <param name="nodeModifier"></param>
    /// <returns></returns>
    private List<Inline> GetTerrainModifierDescription(Node nodeModifier)
    {
        return GetDescriptionForNode(
            nodeModifier,
            leafModifier =>
            {
                string modifierName = _localizationFormatService.GetFormatTextInAll(
                    $"STAT_ADJUSTER_{leafModifier.Key}"
                );
                string modifierFormat = _localizationService.GetValueInAll(
                    $"STAT_ADJUSTER_{leafModifier.Key}_DIFF"
                );
                return
                [
                    new Run { Text = $"{NodeModifierChildrenPrefix}{modifierName}" },
                    GetRun(leafModifier, modifierFormat)
                ];
            }
        );
    }

    /// <summary>
    /// 从 <see cref="LeafModifier"/> 中获取<c>LeafModifier.Value</c>的 <see cref="Run"/>, 并使用<c>modifierFormat</c>设置值的格式
    /// </summary>
    /// <param name="modifier"></param>
    /// <param name="modifierFormat"></param>
    /// <returns></returns>
    private Run GetRun(Leaf modifier, string modifierFormat)
    {
        var brush = _modifierService.GetModifierBrush(modifier, modifierFormat);
        var run = new Run { Text = _modifierService.GetDisplayValue(modifier, modifierFormat), };
        if (brush is not null)
        {
            run.Foreground = brush;
        }
        return run;
    }

    private List<Inline> GetDescriptionForUnknownNode(Node nodeModifier)
    {
        Log.Warn("未知的节点修饰符: {Name}", nodeModifier.Key);
        return GetDescriptionForNode(
            nodeModifier,
            leafModifier =>
            {
                var runs = GetDescriptionForLeaf(leafModifier);
                foreach (var run in runs.AsValueEnumerable().OfType<Run>())
                {
                    run.Text = $"{NodeModifierChildrenPrefix}{run.Text}";
                }

                return runs;
            }
        );
    }

    private List<Inline> GetDescriptionForNode(Node nodeModifier, Func<Leaf, IEnumerable<Inline>> func)
    {
        var inlines = new List<Inline>(nodeModifier.AllArray.Length)
        {
            new Run { Text = $"{_localizationFormatService.GetFormatTextInAll(nodeModifier.Key)}:" },
            new LineBreak()
        };

        foreach (var leafModifier in nodeModifier.LeavesValue)
        {
            inlines.AddRange(func(leafModifier));
            inlines.Add(new LineBreak());
        }

        RemoveLastLineBreak(inlines);
        return inlines;
    }

    /// <summary>
    /// 移除末尾多余的换行
    /// </summary>
    /// <param name="inlines">一段文本的 <see cref="Inline"/> 集合</param>
    private static void RemoveLastLineBreak(List<Inline> inlines)
    {
        if (inlines.Count != 0 && inlines[^1] is LineBreak)
        {
            inlines.RemoveAt(inlines.Count - 1);
        }
    }
}
