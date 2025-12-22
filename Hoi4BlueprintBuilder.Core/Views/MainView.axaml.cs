using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;

namespace Hoi4BlueprintBuilder.Core.Views;

[RegisterSingleton<MainView>]
public sealed partial class MainView : UserControl
{
    private GridLength _rawFileTreeColumnWidth;
    private GridLength _rawSplitterColumnWidth;
    private readonly Animation _settingsButtonAnimation;

    private static readonly GridLength ZeroSize = new(0, GridUnitType.Pixel);

    /// <summary>
    /// 设计器使用
    /// </summary>
    public MainView()
        : this(new FileTreeView()) { }

    public MainView(FileTreeView fileTree)
    {
        InitializeComponent();
        _settingsButtonAnimation = InitializeSettingsButtonAnimation();

        FileTreeView.Content = fileTree;
        FileTreeToggleButton.IsChecked = FileTreeView.IsVisible;
        _rawFileTreeColumnWidth = ContentGrid.ColumnDefinitions[0].Width;
        _rawSplitterColumnWidth = ContentGrid.ColumnDefinitions[1].Width;
        FileTreeToggleButton.IsCheckedChanged += (_, _) =>
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
        };

        SettingsButton.Click += (_, _) => _settingsButtonAnimation.RunAsync(SettingsButton);
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
}
