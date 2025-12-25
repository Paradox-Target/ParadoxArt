using System.Collections.ObjectModel;
using Avalonia.Controls;
using FluentAvalonia.UI.Controls;
using Hoi4BlueprintBuilder.Core.Views;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using ZLinq;

namespace Hoi4BlueprintBuilder.Core.Services;

[RegisterSingleton<TabViewService>]
public sealed class TabViewService(IServiceProvider serviceProvider)
{
    private TabView TabView => _tabView ?? throw new InvalidOperationException("TabViewService 未初始化");
    private TabView? _tabView;
    private Func<double>? _tabContentHeight;
    private Func<double>? _tabContentWight;
    private readonly ObservableCollection<TabViewItem> _openedTabFileItems = [];

    // 高: 32, padding: 8
    private const double TabHeight = 40;
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public void Initialize(TabView tabView, Func<double> tabContentWight, Func<double> tabContentHeight)
    {
        _tabView = tabView;
        tabView.TabItems = _openedTabFileItems;
        _tabContentHeight = tabContentHeight;
        _tabContentWight = tabContentWight;

        _tabView.SelectionChanged += (_, _) => ResizeSelectionTab();
    }

    public void AddTab(ITabViewItem content)
    {
        var openedTabFileItem = _openedTabFileItems.FirstOrDefault(item =>
        {
            var tabViewItem = item.Content as ITabViewItem;
            return tabViewItem?.Equals(content) == true;
        });

        AddTabCore(openedTabFileItem, () => content);
    }

    /// <summary>
    /// 用于添加类似于设置页的 TabView
    /// </summary>
    /// <typeparam name="TType"></typeparam>
    public void AddSingleTabFromIoc<TType>()
        where TType : class, ITabViewItem
    {
        var openedTabFileItem = _openedTabFileItems.FirstOrDefault(item => item.Content is TType);
        AddTabCore(openedTabFileItem, serviceProvider.GetRequiredService<TType>);
    }

    /// <summary>
    /// 添加 TabView
    /// </summary>
    /// <typeparam name="TType">添加的页面类型</typeparam>
    /// <param name="filePath">用来检查是否存在于标签页内的文件路径</param>
    public void AddTabFromIoc<TType>(string filePath)
        where TType : class, ITabViewItem
    {
        var openedTabFileItem = _openedTabFileItems.FirstOrDefault(item =>
        {
            var i = item.Content as TType;
            return i?.FilePath.Equals(filePath, StringComparison.OrdinalIgnoreCase) ?? false;
        });
        AddTabCore(openedTabFileItem, serviceProvider.GetRequiredService<TType>);
    }

    private void AddTabCore(TabViewItem? tabViewItem, Func<ITabViewItem> action)
    {
        if (tabViewItem is null)
        {
            var content = action();
            tabViewItem = new TabViewItem
            {
                Header = content.Header,
                Content = content,
                IconSource = content.TabIcon
            };
            ToolTip.SetTip(tabViewItem, content.ToolTip);
            UpdateControlSize((Control)content);

            _openedTabFileItems.Add(tabViewItem);
        }

        TabView.SelectedItem = tabViewItem;
    }

    public bool RemoveTab(TabViewItem content)
    {
        if (content.Content is IClosed closed)
        {
            closed.Close();
            Log.Debug("释放 {Content}", ((ITabViewItem)content.Content).Header);
        }
        return _openedTabFileItems.Remove(content);
    }

    public void ResizeSelectionTab()
    {
        if (
            _tabContentHeight is null
            || _tabContentWight is null
            || TabView.SelectedItem is not TabViewItem { Content: Control control }
        )
        {
            return;
        }

        UpdateControlSize(control);
    }

    private void UpdateControlSize(Control control)
    {
        if (_tabContentHeight is null || _tabContentWight is null)
        {
            Log.Warn("无法获取外围大小, TabView 内容大小更新失败");
            return;
        }

        control.Width = _tabContentWight.Invoke();
        control.Height = _tabContentHeight.Invoke() - TabHeight;
    }
}
