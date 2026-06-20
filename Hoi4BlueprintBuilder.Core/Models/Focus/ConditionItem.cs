using CommunityToolkit.Mvvm.ComponentModel;

namespace Hoi4BlueprintBuilder.Core.Models.Focus;

/// <summary>
/// 表示一个可独立切换的叶子条件项, 如 <c>has_war = yes</c> 或 <c>[any_country] tag = GER</c>
/// </summary>
public abstract partial class ConditionItem(string scopeName, string nodeContent) : ObservableObject
{
    /// <summary>
    /// 作用域名称, 如 <c>any_country</c>, <c>owner</c> 等, 空字符串表示当前国家作用域
    /// </summary>
    public string ScopeName { get; } = scopeName;

    /// <summary>
    /// 条件内容, 如 <c>has_war = yes</c>, <c>tag = GER</c>
    /// </summary>
    public string NodeContent { get; } = nodeContent;

    /// <summary>
    /// 用于 UI 展示的文本
    /// </summary>
    public string DisplayContent { get; } =
        string.IsNullOrEmpty(scopeName) ? nodeContent : $"[{scopeName}] {nodeContent}";

    /// <summary>
    /// 条件是否被用户标记为 true
    /// </summary>
    [ObservableProperty]
    public partial bool IsEnabled { get; set; }

    public override string ToString() => DisplayContent;
}
