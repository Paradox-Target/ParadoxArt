using System.ComponentModel.Design;
using System.Diagnostics;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;
using FluentAvalonia.UI.Controls;
using Hoi4BlueprintBuilder.Core.Services;
using Hoi4BlueprintBuilder.Core.ViewsModels;
using NLog;

namespace Hoi4BlueprintBuilder.Core.Views;

[RegisterSingleton<MainView>]
public sealed partial class MainView : UserControl
{
    private readonly TabViewService _tabViewService;
    private GridLength _rawFileTreeColumnWidth;
    private GridLength _rawSplitterColumnWidth;
    private readonly Animation _settingsButtonAnimation;

    private static readonly GridLength ZeroSize = new(0, GridUnitType.Pixel);
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// 设计器使用
    /// </summary>
    public MainView()
        : this(new FileTreeView(), new TabViewService(new ServiceContainer()), new MainViewModel()) { }

    public MainView(FileTreeView fileTree, TabViewService tabViewService, MainViewModel mainViewModel)
    {
        InitializeComponent();
        DataContext = mainViewModel;
        _tabViewService = tabViewService;
        _settingsButtonAnimation = InitializeSettingsButtonAnimation();
        FileTreeView.Content = fileTree;

        _tabViewService.Initialize(
            MainTabView,
            () => ContentGrid.ColumnDefinitions[2].ActualWidth,
            () => Bounds.Height
        );
        SizeChanged += MainTabViewOnSizeChanged;
        MainTabView.SizeChanged += MainTabViewOnSizeChanged;

        FileTreeToggleButton.IsCheckedChanged += OnIsCheckedChanged;
        FileTreeToggleButton.IsChecked = FileTreeView.IsVisible;
        SettingsButton.Click += (_, _) =>
        {
            _tabViewService.AddSingleTabFromIoc<AppSettingsView>();
            _settingsButtonAnimation.RunAsync(SettingsButton);
        };
    }

    private void OnIsCheckedChanged(object? o, RoutedEventArgs routedEventArgs)
    {
        bool isOpenedFileTree = FileTreeToggleButton.IsChecked is true;
        if (isOpenedFileTree)
        {
            ContentGrid.ColumnDefinitions[0].Width = _rawFileTreeColumnWidth;
            ContentGrid.ColumnDefinitions[1].Width = _rawSplitterColumnWidth;
            FileTreeView.IsVisible = true;
            MainGridSplitter.IsVisible = true;
        }
        else
        {
            _rawFileTreeColumnWidth = ContentGrid.ColumnDefinitions[0].Width;
            _rawSplitterColumnWidth = ContentGrid.ColumnDefinitions[1].Width;
            ContentGrid.ColumnDefinitions[0].Width = ZeroSize;
            ContentGrid.ColumnDefinitions[1].Width = ZeroSize;
            FileTreeView.IsVisible = false;
            MainGridSplitter.IsVisible = false;
        }
    }

    private void MainTabViewOnSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        e.Handled = true;
        _tabViewService.ResizeSelectionTab();
    }

    private static Animation InitializeSettingsButtonAnimation()
    {
        var animation = new Animation
        {
            Duration = TimeSpan.FromSeconds(1),
            Easing = new QuadraticEaseInOut()
        };
        animation.Children.Add(
            new KeyFrame { Cue = new Cue(0d), Setters = { new Setter(RotateTransform.AngleProperty, 0d) } }
        );
        animation.Children.Add(
            new KeyFrame { Cue = new Cue(1d), Setters = { new Setter(RotateTransform.AngleProperty, 480d) } }
        );
        return animation;
    }

    private void MainTabView_OnTabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args)
    {
        bool isRemoved = _tabViewService.RemoveTab(args.Tab);
        Debug.Assert(isRemoved);
    }
}
