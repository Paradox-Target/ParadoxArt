using System.Collections.Frozen;
using Hoi4BlueprintBuilder.Core.Models.Focus;
using ParadoxPower.CSharpExtensions;
using ParadoxPower.Process;
using ZLinq;

namespace Hoi4BlueprintBuilder.Core.Helpers;

/// <summary>
/// 条件表达式解析与求值工具类
/// </summary>
public static class ConditionHelper
{
    /// <summary>
    /// HOI4 作用域关键字 — 遇到这些时递归进入并传递新的 scopeName
    /// </summary>
    /// <remarks>
    /// 参考 HOI4 Wiki: https://hoi4.paradoxwikis.com/Scopes
    /// 包含国家级, 州级, 角色级等常见作用域
    /// </remarks>
    private static readonly FrozenSet<string> ScopeKeywords = new HashSet<string>
    {
        // 国家级作用域
        "any_country",
        "all_country",
        "any_neighbor_country",
        "all_neighbor_country",
        "any_home_area_neighbor_country",
        "all_home_area_neighbor_country",
        "any_guaranteed_country",
        "all_guaranteed_country",
        "any_allied_country",
        "all_allied_country",
        "any_enemy_country",
        "all_enemy_country",
        "any_occupied_country",
        "all_occupied_country",
        "any_other_country",
        "all_other_country",
        "overlord",
        "faction_leader",
        // 州级作用域
        "any_state",
        "all_state",
        "any_owned_state",
        "all_owned_state",
        "any_controlled_state",
        "all_controlled_state",
        "any_neighbor_state",
        "all_neighbor_state",
        "capital_scope",
        "any_core_state",
        "all_core_state",
        "any_claim_state",
        "all_claim_state",
        // 角色/单位作用域
        "any_army_leader",
        "all_army_leader",
        "any_navy_leader",
        "all_navy_leader",
        "any_unit_leader",
        "all_unit_leader",
        // 特殊引用作用域
        "owner",
        "controller",
        "FROM",
        "ROOT",
        "PREV",
        "THIS",
        "event_target",
        "var"
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 从 ParadoxPower AST Node 中提取条件表达式树, 同时收集所有叶子条件到 collector 中
    /// </summary>
    /// <param name="triggerNode">trigger/allow_branch 的 AST Node</param>
    /// <param name="scopeName">当前作用域名称, 顶层传空字符串</param>
    /// <param name="collector">叶子条件收集器, 会将所有出现的 ConditionItem 去重追加</param>
    /// <returns>条件表达式树</returns>
    public static IConditionExpression ExtractConditionExpression(
        Node triggerNode,
        string scopeName,
        List<ConditionItem> collector
    )
    {
        var items = new List<IConditionExpression>();

        foreach (var child in triggerNode.AllArray)
        {
            if (child.TryGetNode(out var node))
            {
                items.Add(ProcessChildNode(node, scopeName, collector));
            }
            else if (child.TryGetLeaf(out var leaf))
            {
                items.Add(ProcessLeaf(leaf, scopeName, collector));
            }
        }

        return items.Count switch
        {
            0 => new ConditionBool(true),
            1 => items[0],
            _ => new ConditionFolder(ConditionFolderType.And, items)
        };
    }

    private static IConditionExpression ProcessChildNode(
        Node node,
        string scopeName,
        List<ConditionItem> collector
    )
    {
        string key = node.Key;

        // AND 块
        if (key.Equals("AND", StringComparison.OrdinalIgnoreCase))
        {
            return ExtractFolder(node, scopeName, ConditionFolderType.And, collector);
        }

        // OR 块
        if (key.Equals("OR", StringComparison.OrdinalIgnoreCase))
        {
            return ExtractFolder(node, scopeName, ConditionFolderType.Or, collector);
        }

        // NOT 块: HOI4 中 NOT = { A B } 等价于 "A 和 B 都不满足"
        if (key.Equals("NOT", StringComparison.OrdinalIgnoreCase))
        {
            return ExtractFolder(node, scopeName, ConditionFolderType.AndNot, collector);
        }

        // if/else_if/else 块: 取非 limit 子节点作为 AND 分支
        if (
            key.Equals("if", StringComparison.OrdinalIgnoreCase)
            || key.Equals("else_if", StringComparison.OrdinalIgnoreCase)
            || key.Equals("else", StringComparison.OrdinalIgnoreCase)
        )
        {
            return ExtractIfBlock(node, scopeName, collector);
        }

        // limit 块: 递归当前 scope
        if (key.Equals("limit", StringComparison.OrdinalIgnoreCase))
        {
            return ExtractConditionExpression(node, scopeName, collector);
        }

        // 已知作用域关键字: 递归进入新作用域
        if (ScopeKeywords.Contains(key))
        {
            string newScope = string.IsNullOrEmpty(scopeName) ? key : $"{scopeName}.{key}";
            return ExtractConditionExpression(node, newScope, collector);
        }

        // 未知 Node: 整体作为叶子条件
        return CreateLeafFromNode(node, scopeName, collector);
    }

    private static IConditionExpression ExtractFolder(
        Node node,
        string scopeName,
        ConditionFolderType folderType,
        List<ConditionItem> collector
    )
    {
        var items = new List<IConditionExpression>();
        foreach (var child in node.AllArray)
        {
            if (child.TryGetNode(out var childNode))
            {
                items.Add(ProcessChildNode(childNode, scopeName, collector));
            }
            else if (child.TryGetLeaf(out var leaf))
            {
                items.Add(ProcessLeaf(leaf, scopeName, collector));
            }
        }

        return items.Count switch
        {
            0 => new ConditionBool(folderType is ConditionFolderType.And or ConditionFolderType.AndNot),
            1 when folderType is ConditionFolderType.And => items[0],
            1 when folderType is ConditionFolderType.Or => items[0],
            _ => new ConditionFolder(folderType, items)
        };
    }

    private static IConditionExpression ExtractIfBlock(
        Node node,
        string scopeName,
        List<ConditionItem> collector
    )
    {
        var items = new List<IConditionExpression>();
        foreach (var child in node.AllArray)
        {
            if (child.TryGetNode(out var childNode))
            {
                // limit 块中的条件也需要解析
                items.Add(ProcessChildNode(childNode, scopeName, collector));
            }
            else if (child.TryGetLeaf(out var leaf))
            {
                items.Add(ProcessLeaf(leaf, scopeName, collector));
            }
        }

        return items.Count switch
        {
            0 => new ConditionBool(true),
            1 => items[0],
            _ => new ConditionFolder(ConditionFolderType.And, items)
        };
    }

    private static IConditionExpression ProcessLeaf(
        Leaf leaf,
        string scopeName,
        List<ConditionItem> collector
    )
    {
        // 克隆一个, 避免 Parent 引用导致整个 AST 无法被 GC 回收
        var item = new ConditionLeafItem(scopeName, new Leaf(leaf.Key, leaf.Value, leaf.Operator));
        AddToCollector(item, collector);
        return new ConditionLeaf(scopeName, item.NodeContent);
    }

    private static IConditionExpression CreateLeafFromNode(
        Node node,
        string scopeName,
        List<ConditionItem> collector
    )
    {
        // 克隆一个, 避免 Parent 引用导致整个 AST 无法被 GC 回收
        var item = new ConditionNodeItem(scopeName, node.Clone());
        AddToCollector(item, collector);
        return new ConditionLeaf(scopeName, item.NodeContent);
    }

    private static void AddToCollector(ConditionItem item, List<ConditionItem> collector)
    {
        // 去重: 检查是否已存在相同的条件
        foreach (var existing in collector)
        {
            if (existing.ScopeName == item.ScopeName && existing.NodeContent == item.NodeContent)
            {
                return;
            }
        }
        collector.Add(item);
    }

    /// <summary>
    /// 递归求值条件表达式树
    /// </summary>
    /// <param name="expression">条件表达式</param>
    /// <param name="trueSet">当前为 true 的条件集合 (ScopeName, NodeContent)</param>
    /// <returns>条件整体是否为 true</returns>
    public static bool Evaluate(
        IConditionExpression expression,
        HashSet<(string ScopeName, string NodeContent)> trueSet
    )
    {
        return expression switch
        {
            ConditionBool b => b.Value,
            ConditionLeaf leaf => trueSet.Contains((leaf.ScopeName, leaf.NodeContent)),
            ConditionFolder folder => EvaluateFolder(folder, trueSet),
            _ => false
        };
    }

    private static bool EvaluateFolder(
        ConditionFolder folder,
        HashSet<(string ScopeName, string NodeContent)> trueSet
    )
    {
        return folder.Type switch
        {
            // AND: 所有子项为 true
            ConditionFolderType.And
                => folder.Items.AsValueEnumerable().All(item => Evaluate(item, trueSet)),
            // OR: 任一子项为 true
            ConditionFolderType.Or
                => folder.Items.AsValueEnumerable().Any(item => Evaluate(item, trueSet)),
            // NOT (AndNot): 所有子项为 false (HOI4 NOT 语义: 块内所有条件都不满足)
            ConditionFolderType.AndNot
                => folder.Items.AsValueEnumerable().All(item => !Evaluate(item, trueSet)),
            // OrNot: 任一子项为 false
            ConditionFolderType.OrNot
                => folder.Items.AsValueEnumerable().Any(item => !Evaluate(item, trueSet)),
            _ => false
        };
    }
}
