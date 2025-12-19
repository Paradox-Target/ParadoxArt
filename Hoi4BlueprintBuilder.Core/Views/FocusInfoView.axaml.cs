using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Hoi4BlueprintBuilder.Core.Constants;
using Hoi4BlueprintBuilder.Core.Helpers;
using Hoi4BlueprintBuilder.Core.Models.Focus;
using Hoi4BlueprintBuilder.Core.Services;
using Hoi4BlueprintBuilder.Core.ViewsModels;
using Microsoft.Extensions.DependencyInjection;
using NLog;

namespace Hoi4BlueprintBuilder.Core.Views;

public sealed partial class FocusInfoView : UserControl
{
    public static readonly StyledProperty<bool> IsOpenProperty = AvaloniaProperty.Register<
        FocusInfoView,
        bool
    >(nameof(IsOpen), false);

    public bool IsOpen
    {
        get => GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    private FocusInfoViewModel? _viewModel;

    private static readonly ImageService ImageService =
        App.Current.Services.GetRequiredService<ImageService>();
    private static readonly FileResourceService FileResourceService =
        App.Current.Services.GetRequiredService<FileResourceService>();
    private static readonly NotificationService NotificationService =
        App.Current.Services.GetRequiredService<NotificationService>();
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    static FocusInfoView()
    {
        IsOpenProperty.Changed.AddClassHandler<FocusInfoView>(OnIsOpenChanged);
    }

    public FocusInfoView()
    {
        InitializeComponent();

        // 设置 DataContext 防止运行时提示绑定错误
        _viewModel = new FocusInfoViewModel(new FocusNode(string.Empty, FocusType.Unknown));
        DataContext = _viewModel;
        DataContextChanged += FocusInfoView_DataContextChanged;

        // 点击信息面板时阻止事件冒泡, 导致点击FocusInfoView时关闭面板
        PointerPressed += (_, args) =>
        {
            args.Handled = true;
        };

        // 设置拖放事件
        AddHandler(DragDrop.DropEvent, FocusIcon_OnDrop);

        // 绑定 CompletionRewardEditor 文本变更
        // CompletionRewardEditor.GetObservable(TextBox.TextProperty).Subscribe(OnCompletionRewardTextChanged);

        // 绑定 LostFocus 事件以模拟 UpdateSourceTrigger=LostFocus
        IdTextBox.LostFocus += IdTextBox_OnLostFocus;
        DescriptionTextBox.LostFocus += DescriptionTextBox_OnLostFocus;
    }

    private void FocusInfoView_DataContextChanged(object? sender, EventArgs e)
    {
        // 清理原来的 ViewModel 资源
        if (_viewModel is not null)
        {
            _viewModel.Dispose();
            _viewModel.FocusNode.PropertyChanged -= FocusNodeOnPropertyChanged;
            _viewModel = null;
        }

        if (DataContext is not FocusInfoViewModel newViewModel)
        {
            return;
        }

        _viewModel = newViewModel;
        newViewModel.FocusNode.PropertyChanged += FocusNodeOnPropertyChanged;
        CompletionRewardEditor.Text = newViewModel.FocusNode.CompletionReward;

        if (!string.IsNullOrEmpty(newViewModel.FocusNode.Icon))
        {
            SetImage(ImageService.GetFocusIconByName(newViewModel.FocusNode.Icon));
        }
    }

    private void OnCompletionRewardTextChanged(string? text)
    {
        if (_viewModel is not null)
        {
            _viewModel.FocusNode.CompletionReward = text ?? string.Empty;
        }
    }

    private void IdTextBox_OnLostFocus(object? sender, RoutedEventArgs e)
    {
        // Avalonia 的 TextBox 绑定默认是 PropertyChanged 模式
        // 这里手动触发 ViewModel 更新以模拟 LostFocus 行为
        if (_viewModel is not null && sender is TextBox textBox)
        {
            _viewModel.IdText = textBox.Text ?? string.Empty;
        }
    }

    private void DescriptionTextBox_OnLostFocus(object? sender, RoutedEventArgs e)
    {
        if (_viewModel is not null && sender is TextBox textBox)
        {
            _viewModel.DescriptionText = textBox.Text ?? string.Empty;
        }
    }

    private void FocusNodeOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(FocusNode.Icon))
        {
            var focusNode = (FocusNode?)sender;
            SetImage(ImageService.GetFocusIconByName(focusNode!.Icon));
        }
    }

    private static void OnIsOpenChanged(FocusInfoView view, AvaloniaPropertyChangedEventArgs e)
    {
        bool isOpen = e.GetNewValue<bool>();
        view.IsVisible = isOpen;
        int zIndex = isOpen ? FocusMapConstants.FocusInfoZIndex : -1;
        view.ZIndex = zIndex;
    }

    private void FocusIcon_OnDrop(object? sender, DragEventArgs e)
    {
        var files = e.DataTransfer.TryGetFiles();
        if (files is null)
        {
            return;
        }

        // 当有多个文件时, 只使用第一个文件
        string? filePath = files.FirstOrDefault()?.Path?.LocalPath;
        if (string.IsNullOrEmpty(filePath))
        {
            return;
        }

        if (ImageHelper.IsValidFocusImageFormat(filePath))
        {
            var result = FileResourceService.RegisterFocusIcon(filePath);
            if (string.IsNullOrEmpty(result.SpriteName) || string.IsNullOrEmpty(result.DestFilePath))
            {
                NotificationService.Show("添加图标失败");
                return;
            }

            SetImage(ImageService.GetImageSource(result.SpriteName, result.DestFilePath));
            if (_viewModel is not null)
            {
                _viewModel.FocusNode.Icon = result.SpriteName;
            }

            Log.Info("添加图标成功: {Name}", result.SpriteName);
            NotificationService.Show(result.IsConvertToDds ? "添加图标成功, 图片已自动转换为 DDS 格式" : "添加图标成功");
        }
    }

    private void SetImage(Bitmap? bitmap)
    {
        FocusIcon.Source = bitmap;
        FocusIcon.Width = bitmap?.PixelSize.Width ?? 0;
        FocusIcon.Height = bitmap?.PixelSize.Height ?? 0;
    }
}
