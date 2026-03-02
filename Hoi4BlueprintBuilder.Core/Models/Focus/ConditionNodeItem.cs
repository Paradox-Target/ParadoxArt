using ParadoxPower.CSharpExtensions;
using ParadoxPower.Process;

namespace Hoi4BlueprintBuilder.Core.Models.Focus;

/// <summary>
/// 由 AST 子节点 (<see cref="Node"/>) 派生的条件项, 如 <c>custom_trigger_tooltip = { ... }</c>
/// </summary>
public sealed class ConditionNodeItem(string scopeName, Node node)
    : ConditionItem(scopeName, node.ToScript().TrimEnd('\n'))
{
    /// <summary>
    /// 原始 AST 节点
    /// </summary>
    public Node Node { get; } = node;
}
