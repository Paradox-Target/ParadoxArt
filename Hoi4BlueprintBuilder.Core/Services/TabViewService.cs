using Avalonia;
using Avalonia.Collections;
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
    public ITabViewItem? CurrentItem => ((FATabViewItem?)_tabView?.SelectedItem)?.Content as ITabViewItem;
    public event Action<ITabViewItem?>? CurrentItemChanged;

    private FATabView TabView => _tabView ?? throw new InvalidOperationException("TabViewService 未初始化");
    private FATabView? _tabView;
    private Func<double>? _tabContentHeight;
    private Func<double>? _tabContentWight;
    private readonly AvaloniaList<FATabViewItem> _openedTabFileItems = [];

    // 高: 32, padding: 8
    private const double TabHeight = 40;
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public void Initialize(FATabView tabView, Func<double> tabContentWight, Func<double> tabContentHeight)
    {
        _tabView = tabView;
        tabView.TabItemsSource = _openedTabFileItems;
        _tabContentHeight = tabContentHeight;
        _tabContentWight = tabContentWight;

        _tabView.SelectionChanged += (_, _) =>
        {
            ResizeSelectionTab();
            CurrentItemChanged?.Invoke(CurrentItem);
        };
        var project = serviceProvider.GetRequiredService<ProjectConfigService>();
        var pathService = serviceProvider.GetRequiredService<ProjectPathService>();
        App.Current.OnExitBefore += (_, _) =>
        {
            project.OpenedFiles.Clear();

            foreach (var tabViewItem in _openedTabFileItems)
            {
                var item = (ITabViewItem?)tabViewItem.Content!;
                project.OpenedFiles.Add(pathService.GetRelativeModPath(item.FilePath));
            }
        };
    }

    public void AddTab(ITabViewItem content)
    {
        var openedTabFileItem = _openedTabFileItems
            .AsValueEnumerable()
            .FirstOrDefault(item =>
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
        var openedTabFileItem = _openedTabFileItems
            .AsValueEnumerable()
            .FirstOrDefault(item => item.Content is TType);
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
        var openedTabFileItem = _openedTabFileItems
            .AsValueEnumerable()
            .FirstOrDefault(item =>
            {
                var i = item.Content as TType;
                return i?.FilePath.Equals(filePath, StringComparison.OrdinalIgnoreCase) ?? false;
            });
        AddTabCore(openedTabFileItem, serviceProvider.GetRequiredService<TType>);
    }

    private void AddTabCore(FATabViewItem? tabViewItem, Func<ITabViewItem> action)
    {
        if (tabViewItem is null)
        {
            var content = action();
            tabViewItem = new FATabViewItem
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

    public bool RemoveTab(FATabViewItem content)
    {
        if (content.Content is IClosed closed)
        {
            closed.Close();
            Log.Debug("释放 {Content}", ((ITabViewItem)content.Content).Header);
        }

        if (content.Content is StyledElement { DataContext: IClosed c })
        {
            c.Close();
            Log.Debug("释放 {Content} DataContext", ((ITabViewItem)content.Content).Header);
        }
        return _openedTabFileItems.Remove(content);
    }

    public void ResizeSelectionTab()
    {
        if (
            _tabContentHeight is null
            || _tabContentWight is null
            || TabView.SelectedItem is not FATabViewItem { Content: Control control }
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
