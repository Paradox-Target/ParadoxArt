using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Xml;
using Hoi4BlueprintEditor.Constants;
using Hoi4BlueprintEditor.Helpers;
using Hoi4BlueprintEditor.Models.Focus;
using Hoi4BlueprintEditor.Services;
using Hoi4BlueprintEditor.ViewsModels;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using Microsoft.Extensions.DependencyInjection;
using NLog;

namespace Hoi4BlueprintEditor.Views;

public sealed partial class FocusInfoView : UserControl
{
    // TODO: 改变窗口大小或者移动窗口时关闭信息面板? (避免位置错乱)
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

    private FocusInfoViewModel _viewModel;

    private static readonly ImageService ImageService =
        App.Current.Services.GetRequiredService<ImageService>();
    private static readonly FileResourceService FileResourceService =
        App.Current.Services.GetRequiredService<FileResourceService>();
    private static readonly NotificationService NotificationService =
        App.Current.Services.GetRequiredService<NotificationService>();
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public FocusInfoView()
    {
        InitializeComponent();

        // 设置 DataContext 防止运行时提示绑定错误
        _viewModel = new FocusInfoViewModel(new FocusNode(string.Empty, FocusType.Unknown));
        DataContext = _viewModel;
        DataContextChanged += FocusInfoView_DataContextChanged;
        // 点击信息面板时阻止事件冒泡, 导致点击FocusInfoView时关闭面板
        MouseLeftButtonDown += static (_, args) =>
        {
            args.Handled = true;
        };
        SetSyntaxHighlighting();
    }

    private void SetSyntaxHighlighting()
    {
        var assembly = Assembly.GetExecutingAssembly();
        const string resourceName = "Hoi4BlueprintEditor.Assets.Hoi4Syntax.xshd";
        using var file = assembly.GetManifestResourceStream(resourceName);
        if (file is not null)
        {
            using var reader = new XmlTextReader(file);
            CompletionRewardEditor.SyntaxHighlighting = HighlightingLoader.Load(
                reader,
                HighlightingManager.Instance
            );
        }
    }

    private void FocusInfoView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is not FocusInfoViewModel viewModel)
        {
            return;
        }

        _viewModel = viewModel;
        if (e.OldValue is FocusInfoViewModel oldViewModel)
        {
            oldViewModel.FocusNode.PropertyChanged -= FocusNodeOnPropertyChanged;
            CompletionRewardEditor.Document.TextChanged -= OnDocumentTextChanged;
            oldViewModel.Dispose();
        }

        viewModel.FocusNode.PropertyChanged += FocusNodeOnPropertyChanged;

        if (!string.IsNullOrEmpty(viewModel.FocusNode.Icon))
        {
            SetImage(ImageService.GetFocusIconByName(viewModel.FocusNode.Icon));
        }

        CompletionRewardEditor.Document.Text = viewModel.FocusNode.CompletionReward;
        CompletionRewardEditor.Document.TextChanged += OnDocumentTextChanged;
    }

    private void OnDocumentTextChanged(object? o, EventArgs eventArgs)
    {
        _viewModel.FocusNode.CompletionReward = CompletionRewardEditor.Document.Text;
    }

    private void FocusNodeOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(FocusNode.Icon))
        {
            var focusNode = (FocusNode?)sender;
            SetImage(ImageService.GetFocusIconByName(focusNode!.Icon));
        }
    }

    private static void OnIsOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FocusInfoView view)
        {
            bool isOpen = (bool)e.NewValue;
            view.Visibility = isOpen ? Visibility.Visible : Visibility.Collapsed;
            int zIndex = isOpen ? FocusMapConstants.FocusInfoZIndex : -1;
            Panel.SetZIndex(view, zIndex);
        }
    }

    private void FocusIcon_OnDrop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop) is not string[] files)
        {
            return;
        }

        // 当有多个文件时, 只使用第一个文件
        string filePath = files[0];

        if (ImageHelper.IsValidFocusImageFormat(filePath))
        {
            var result = FileResourceService.RegisterFocusIcon(filePath);
            if (string.IsNullOrEmpty(result.IconId) || string.IsNullOrEmpty(result.DestFilePath))
            {
                NotificationService.Show("添加图标失败");
                return;
            }

            //TODO: 修改 .gfx 文件后 SpriteService 能否及时获取到图标?
            SetImage(ImageService.GetImageSource(result.DestFilePath));
            var viewModel = (FocusInfoViewModel)DataContext;
            viewModel.FocusNode.Icon = result.IconId;

            Log.Info("添加图标成功: {Name}", result.IconId);
            NotificationService.Show(result.IsConvertToDds ? "添加图标成功, 图片已自动转换为 DDS 格式" : "添加图标成功");
        }
    }

    private void SetImage(BitmapSource? bitmapSource)
    {
        FocusIcon.Source = bitmapSource;
        FocusIcon.Width = bitmapSource?.PixelWidth ?? 0;
        FocusIcon.Height = bitmapSource?.PixelHeight ?? 0;
    }
}
