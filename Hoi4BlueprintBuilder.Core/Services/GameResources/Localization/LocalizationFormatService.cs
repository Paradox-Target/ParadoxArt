using System.Diagnostics.CodeAnalysis;
using Avalonia.Media;
using Hoi4BlueprintBuilder.Core.Extensions;
using Hoi4BlueprintBuilder.Core.Infrastructure.Parser;
using Hoi4BlueprintBuilder.Core.Models;
using Hoi4BlueprintBuilder.Core.Models.Focus;
using ZLinq;

namespace Hoi4BlueprintBuilder.Core.Services.GameResources.Localization;

[RegisterSingleton<LocalizationFormatService>]
public sealed class LocalizationFormatService
{
    private readonly LocalizationTextColorsService _localizationTextColorsService;
    private readonly LocalizationService _localizationService;

    public LocalizationFormatService(
        LocalizationTextColorsService localizationTextColorsService,
        LocalizationService localizationService
    )
    {
        _localizationTextColorsService = localizationTextColorsService;
        _localizationService = localizationService;
        FocusNode.SetLocalizationFormatService(this);
    }

    /// <summary>
    /// 根据 <c>key</c> 获取格式化后的文本
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns>格式化后的文本, 如果找到值, 返回<c>true</c>, 反之返回<c>false</c></returns>
    public bool TryGetFormatText(string key, [NotNullWhen(true)] out string? value)
    {
        if (_localizationService.TryGetValue(key, out value))
        {
            value = GetFormatTextByText(value);
            return true;
        }

        return false;
    }

    public bool TryGetFormatText(string key, GameLanguage language, [NotNullWhen(true)] out string? value)
    {
        if (_localizationService.TryGetValue(key, language, out value))
        {
            value = GetFormatTextByText(value);
            return true;
        }

        return false;
    }

    /// <summary>
    /// 根据 <c>key</c> 获取格式化后的文本
    /// </summary>
    /// <param name="key"></param>
    /// <returns>格式化后的文本, 如果未找到值, 则返回<c>key</c></returns>
    public string GetFormatText(string key)
    {
        return TryGetFormatText(key, out string? value) ? value : key;
    }

    public string GetFormatText(string key, string placeholder, int value)
    {
        if (!_localizationService.TryGetValue(key, out string? localizationText))
        {
            return key;
        }

        var result = new List<TextFormatInfo>();
        ParseFormat(localizationText, result, placeholder, value);

        return result.AsValueEnumerable().Select(static info => info.DisplayText).JoinToString(string.Empty);
    }

    /// <summary>
    /// 根据 <c>key</c> 获取格式化后的文本信息, 并替换指定占位符
    /// </summary>
    /// <param name="key">本地化键</param>
    /// <param name="placeholder">要替换的占位符名称</param>
    /// <param name="value">替换值</param>
    /// <returns>格式化后的文本信息集合</returns>
    public IReadOnlyCollection<TextFormatInfo> GetFormatTextInfoByKey(
        string key,
        string? placeholder = null,
        int value = 0
    )
    {
        if (!_localizationService.TryGetValue(key, out string? localizationText))
        {
            return [new TextFormatInfo(key, null)];
        }

        var result = new List<TextFormatInfo>();
        ParseFormat(localizationText, result, placeholder, value);

        return result;
    }

    public string GetFormatText(string key, GameLanguage language)
    {
        return TryGetFormatText(key, language, out string? value) ? value : key;
    }

    /// <summary>
    /// 获取格式化后的文本
    /// </summary>
    /// <param name="text"></param>
    /// <returns>一个格式化后被拼接的文本</returns>
    private string GetFormatTextByText(string text)
    {
        return GetFormatTextInfo(text)
            .AsValueEnumerable()
            .Select(static info => info.DisplayText)
            .JoinToString(string.Empty);
    }

    /// <summary>
    /// 获取格式化后的文本信息, 不包含 Icon, 如果解析文本颜色失败, 则统一使用黑色
    /// </summary>
    /// <param name="text">文本</param>
    /// <returns>一个集合, 包含格式化后的文本</returns>
    /// <remarks>
    /// 现支持
    /// 1. 文本颜色格式
    /// 2. 对其他本地化键的引用
    /// 3. Icon 引用
    /// </remarks>
    public IReadOnlyCollection<TextFormatInfo> GetFormatTextInfo(string text)
    {
        var result = new List<TextFormatInfo>(4);

        ParseFormat(text, result);

        return result;
    }

    private void ParseFormatToList(
        IEnumerable<LocalizationFormatInfo> formats,
        List<TextFormatInfo> result,
        string? placeholder = null,
        int value = 0
    )
    {
        foreach (var format in formats)
        {
            if (format.Type == LocalizationFormatType.Placeholder)
            {
                var span = format.Text.AsSpan();
                int index = span.IndexOf('|');

                // 如果占位符匹配, 则替换为传入的值
                if (
                    placeholder is not null
                    && (
                        format.Text.EqualsIgnoreCase(placeholder)
                        || (
                            index != -1
                            && span[..index].Equals(placeholder, StringComparison.OrdinalIgnoreCase)
                        )
                    )
                )
                {
                    var formatSpecifier = span[(index + 1)..];
                    Color? color = null;
                    if (
                        formatSpecifier.Length == 1
                        && _localizationTextColorsService.TryGetColor(formatSpecifier[0], out var colorInfo)
                    )
                    {
                        color = colorInfo.Color;
                    }
                    result.Add(new TextFormatInfo(value.ToString(), color));
                    continue;
                }

                // 忽略其他
                if (index != -1 || format.Text == "VALUE" || format.Text == "VAL")
                {
                    continue;
                }

                // 递归处理所有本地化引用
                string text = _localizationService.GetValue(format.Text);
                ParseFormat(text, result, placeholder, value);
            }
            else if (format.Type == LocalizationFormatType.Icon)
            {
                // 暂时不处理 Icon 格式
            }
            else
            {
                result.AddRange(GetColorText(format, placeholder, value));
            }
        }
    }

    private void ParseFormat(
        string text,
        List<TextFormatInfo> result,
        string? placeholder = null,
        int value = 0
    )
    {
        if (LocalizationFormatParser.TryParse(text, out var formats))
        {
            ParseFormatToList(formats, result, placeholder, value);
        }
        else
        {
            result.Add(new TextFormatInfo(text, null));
        }
    }

    /// <summary>
    /// 尝试将文本解析为 <see cref="TextFormatInfo"/>, 并使用 <see cref="LocalizationFormatInfo"/> 中指定的颜色, 如果颜色不存在, 则使用默认颜色
    /// </summary>
    /// <param name="format">文本格式信息</param>
    /// <param name="placeholder">占位符</param>
    /// <param name="value">值</param>
    /// <returns></returns>
    private IEnumerable<TextFormatInfo> GetColorText(
        LocalizationFormatInfo format,
        string? placeholder = null,
        int value = 0
    )
    {
        Color? color = null;
        ReadOnlySpan<char> text = format.Text;
        if (format.Type == LocalizationFormatType.TextWithColor)
        {
            if (string.IsNullOrEmpty(format.Text))
            {
                return [new TextFormatInfo(string.Empty, null)];
            }

            if (_localizationTextColorsService.TryGetColor(text[0], out var colorInfo))
            {
                color = colorInfo.Color;
                text = text[1..];
            }

            // 处理嵌套在着色语法中的其他语法使用 ($$ 转义, \n 换行, 占位符, 图标, 键引用等)
            if (LocalizationFormatParser.TryParse(text, out var formatInfos))
            {
                var list = new List<TextFormatInfo>();
                ParseFormatToList(formatInfos, list, placeholder, value);
                for (int i = 0; i < list.Count; i++)
                {
                    var info = list[i];
                    list[i] = new TextFormatInfo(info.DisplayText, info.Color ?? color);
                }

                return list;
            }
        }

        return [new TextFormatInfo(text.ToString(), color)];
    }
}
