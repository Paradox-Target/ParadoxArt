using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using CommunityToolkit.Mvvm.Messaging;
using Hoi4BlueprintEditor.Messages;
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

        // 主窗口改变时关闭信息面板
        WeakReferenceMessenger.Default.Register<MainWindowStateChangeMessage>(this, MainWindowStateChanged);
        WeakReferenceMessenger.Default.Register<MainWindowDeactivatedMessage>(
            this,
            (_, _) => IsOpen = false
        );
        FocusInfoPopup.CustomPopupPlacementCallback = CustomPopupPlacement;
        DataContextChanged += FocusInfoView_DataContextChanged;
    }

    private void FocusInfoView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is not FocusInfoViewModel viewModel)
        {
            return;
        }

        if (string.IsNullOrEmpty(viewModel.IconPath))
        {
            return;
        }

        var bitmapSource = ImageService.GetImageSource(viewModel.IconPath);
        FocusIcon.Source = bitmapSource;
        FocusIcon.Width = bitmapSource?.PixelWidth ?? 0;
        FocusIcon.Height = bitmapSource?.PixelHeight ?? 0;
    }

    private static void OnIsOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FocusInfoView view)
        {
            view.FocusInfoPopup.IsOpen = (bool)e.NewValue;
        }
    }

    private static CustomPopupPlacement[] CustomPopupPlacement(Size popupSize, Size targetSize, Point offset)
    {
        var customPopupPlacement = new CustomPopupPlacement(
            new Point(targetSize.Width - popupSize.Width, (targetSize.Height - popupSize.Height) / 2),
            PopupPrimaryAxis.Vertical
        );
        return [customPopupPlacement];
    }

    // TODO: 改变窗口大小或者移动窗口时关闭信息面板? (避免位置错乱)
    private void MainWindowStateChanged(object _, MainWindowStateChangeMessage message)
    {
        if (message.Sender is not Window window)
        {
            return;
        }

        if (window.WindowState == WindowState.Minimized)
        {
            IsOpen = false;
        }
    }
}
