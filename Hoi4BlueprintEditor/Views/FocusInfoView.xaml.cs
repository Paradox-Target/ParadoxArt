using System.Windows;
using System.Windows.Controls;
using Hoi4BlueprintEditor.Services;
using Hoi4BlueprintEditor.ViewsModels;
using Microsoft.Extensions.DependencyInjection;

namespace Hoi4BlueprintEditor.Views;

public sealed partial class FocusInfoView : UserControl
{
    public static readonly DependencyProperty IsOpenProperty = DependencyProperty.Register(
        nameof(IsOpen),
        typeof(bool),
        typeof(FocusInfoView),
        new PropertyMetadata(false, OnIsOpenChanged)
    );

    public bool IsOpen
    {
        get => (bool)GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    private static readonly ImageService ImageService =
        App.Current.Services.GetRequiredService<ImageService>();

    public FocusInfoView()
    {
        InitializeComponent();

        DataContextChanged += FocusInfoView_DataContextChanged;
    }

    private void FocusInfoView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is not FocusInfoViewModel viewModel)
        {
            return;
        }

        if (string.IsNullOrEmpty(viewModel.FocusNode.Icon))
        {
            return;
        }

        var bitmapSource = ImageService.GetFocusIconByName(viewModel.FocusNode.Icon);
        FocusIcon.Source = bitmapSource;
        FocusIcon.Width = bitmapSource?.PixelWidth ?? 0;
        FocusIcon.Height = bitmapSource?.PixelHeight ?? 0;
    }

    private static void OnIsOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FocusInfoView view)
        {
            view.Visibility = (bool)e.NewValue ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    // TODO: 改变窗口大小或者移动窗口时关闭信息面板? (避免位置错乱)

}
