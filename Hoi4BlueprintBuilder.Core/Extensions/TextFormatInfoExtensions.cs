using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Hoi4BlueprintBuilder.Core.Models;
using Hoi4BlueprintBuilder.Core.Services.GameResources.Localization;
using ZLinq;

namespace Hoi4BlueprintBuilder.Core.Extensions;

public static class TextFormatInfoExtensions
{
    private static readonly Dictionary<Color, ImmutableSolidColorBrush> ColorCache = new();

    public static TextBlock ToTextBlock(this IEnumerable<Inline> info)
    {
        var textBlock = new TextBlock();
        textBlock.Inlines ??= new InlineCollection();
        textBlock.Inlines.AddRange(info);
        return textBlock;
    }

    public static IReadOnlyCollection<Inline> GetFormatTextWithColor(
        this LocalizationFormatService service,
        string key,
        string? placeholder = null,
        int value = 0
    )
    {
        return service.GetFormatTextInfoByKey(key, placeholder, value).ToInlines();
    }

    /// <summary>
    /// 将 <see cref="TextFormatInfo"/> 集合转换为 Avalonia <see cref="Inline"/> 集合.
    /// </summary>
    private static List<Inline> ToInlines(this IReadOnlyCollection<TextFormatInfo> infos)
    {
        if (infos.AsValueEnumerable().All(x => x.Color is null))
        {
            return
            [
                new Run(
                    infos
                        .AsValueEnumerable()
                        .Select(static info => info.DisplayText)
                        .JoinToString(string.Empty)
                )
            ];
        }

        var inlines = new List<Inline>(infos.Count);
        foreach (var info in infos)
        {
            var run = new Run(info.DisplayText);
            if (info.Color is not null)
            {
                var color = info.Color.Value;
                ImmutableSolidColorBrush? brush;
                lock (ColorCache)
                {
                    if (!ColorCache.TryGetValue(color, out brush))
                    {
                        brush = new ImmutableSolidColorBrush(color);
                        ColorCache[color] = brush;
                    }
                }
                run.Foreground = brush;
            }
            inlines.Add(run);
        }
        return inlines;
    }
}
