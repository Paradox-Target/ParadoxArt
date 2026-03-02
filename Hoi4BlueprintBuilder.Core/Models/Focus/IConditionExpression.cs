namespace Hoi4BlueprintBuilder.Core.Models.Focus;

/// <summary>
/// 条件文件夹类型, 对应 HOI4 逻辑运算符
/// </summary>
public enum ConditionFolderType
{
    /// <summary>
    /// 与运算: 所有子项均为 true 时为 true
    /// </summary>
    And,

    /// <summary>
    /// 或运算: 任一子项为 true 时为 true
    /// </summary>
    Or,

    /// <summary>
    /// NOT 语义: 所有子项均为 false 时为 true (即 NOT(OR(...)))
    /// </summary>
    /// <remarks>HOI4 中 <c>NOT = { A B }</c> 等价于 "A 和 B 都不满足"</remarks>
    AndNot,

    /// <summary>
    /// 取反或: 任一子项为 false 时为 true (即 NOT(AND(...)))
    /// </summary>
    OrNot
}

/// <summary>
/// 条件表达式树的接口
/// </summary>
public interface IConditionExpression;

/// <summary>
/// 叶子条件, 引用一个 <see cref="ConditionItem"/>
/// </summary>
/// <param name="ScopeName">作用域名称</param>
/// <param name="NodeContent">条件内容文本</param>
public sealed record ConditionLeaf(string ScopeName, string NodeContent) : IConditionExpression;

/// <summary>
/// 逻辑组合条件 (AND/OR/NOT)
/// </summary>
/// <param name="Type">逻辑类型</param>
/// <param name="Items">子表达式列表</param>
public sealed record ConditionFolder(ConditionFolderType Type, IReadOnlyList<IConditionExpression> Items)
    : IConditionExpression;

/// <summary>
/// 布尔常量 (用于表示始终为 true/false 的条件或化简结果)
/// </summary>
/// <param name="Value">常量值</param>
public sealed record ConditionBool(bool Value) : IConditionExpression;
