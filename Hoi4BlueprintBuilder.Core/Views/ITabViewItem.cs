using FluentAvalonia.UI.Controls;

namespace Hoi4BlueprintBuilder.Core.Views;

public interface ITabViewItem
{
    /// <summary>
    /// 选项卡显示的名称
    /// </summary>
    string Header { get; }

    /// <summary>
    /// 文件路径, 用来在 TabView 中判断是否存在
    /// </summary>
    /// <remarks>
    /// 设置UI会使用一个不存在的路径
    /// </remarks>
    string FilePath { get; }

    string ToolTip { get; }

    /// <summary>
    /// 选项卡左侧显示的图标
    /// </summary>
    IconSource? TabIcon => null;

    bool Equals(ITabViewItem? other)
    {
        return FilePath == other?.FilePath;
    }
}
