using ParadoxPower.Process;

namespace Hoi4BlueprintBuilder.Core.Models.Focus;

/// <summary>
/// 由 AST 叶子节点 (<see cref="Leaf"/>) 派生的条件项, 如 <c>has_war = yes</c>
/// </summary>
public sealed class ConditionLeafItem(string scopeName, Leaf leaf)
    : ConditionItem(scopeName, $"{leaf.Key} = {leaf.ValueText}")
{
    /// <summary>
    /// 原始 AST 叶子节点
    /// </summary>
    public Leaf Leaf { get; } = leaf;
}
