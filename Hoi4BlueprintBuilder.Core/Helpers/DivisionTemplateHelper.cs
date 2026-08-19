using Hoi4BlueprintBuilder.Core.Extensions;
using ParadoxPower.CSharpExtensions;
using ParadoxPower.Process;

namespace Hoi4BlueprintBuilder.Core.Helpers;

public static class DivisionTemplateHelper
{
    /// <summary>
    /// 获取 AST 根节点下所有的 <c>division_template</c> 节点。
    /// </summary>
    /// <remarks>
    /// <c>history/units</c> 文件中的部队模板定义在文件顶层, 因此只遍历根节点的子节点。
    /// </remarks>
    /// <param name="rootNode">文件解析后的根节点</param>
    /// <returns>文件中的部队模板节点</returns>
    public static IEnumerable<Node> GetDivisionTemplates(Node rootNode)
    {
        foreach (var child in rootNode.AllArray)
        {
            if (
                child.TryGetNode(out var node)
                && node.Key.EqualsIgnoreCase(Keywords.DivisionTemplate)
            )
            {
                yield return node;
            }
        }
    }
}
