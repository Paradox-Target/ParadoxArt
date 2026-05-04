using System.ComponentModel;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.Messaging;
using Hoi4BlueprintBuilder.Core.Constants;
using Hoi4BlueprintBuilder.Core.Helpers;
using Hoi4BlueprintBuilder.Core.Messages;
using Hoi4BlueprintBuilder.Core.Models;
using Hoi4BlueprintBuilder.Core.Models.Focus;
using Hoi4BlueprintBuilder.Core.Services;
using Hoi4BlueprintBuilder.Core.ViewsModels;
using Microsoft.Extensions.DependencyInjection;
using NLog;

namespace Hoi4BlueprintBuilder.Core.Views;

public sealed partial class FocusInfoView : UserControl
{
    public const int MinCardWidth = 200;
    public const int MinCardHeight = 200;

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
    private FocusNode? focusNode => _viewModel?.FocusNode;

    private readonly ImageService _imageService = App.Current.Services.GetRequiredService<ImageService>();
    private readonly FileResourceService _fileResourceService =
        App.Current.Services.GetRequiredService<FileResourceService>();
    private readonly NotificationService _notificationService =
        App.Current.Services.GetRequiredService<NotificationService>();
    private readonly SettingsService _settingsService =
        App.Current.Services.GetRequiredService<SettingsService>();
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    static FocusInfoView()
    {
        IsOpenProperty.Changed.AddClassHandler<FocusInfoView>(OnIsOpenChanged);
    }

    public FocusInfoView()
    {
        InitializeComponent();

        Width = _settingsService.FocusInfoCardWidth;
        Height = _settingsService.FocusInfoCardHeight;

        CompletionRewardEditor.SetGrammar(".txt");
        // 设置 DataContext 防止运行时提示绑定错误
        _viewModel = new FocusInfoViewModel(new FocusNode(string.Empty, FocusType.Unknown));
        DataContext = _viewModel;
        DataContextChanged += FocusInfoView_DataContextChanged;

        // 点击信息面板时阻止事件冒泡, 导致点击FocusInfoView时关闭面板
        PointerPressed += (_, args) =>
        {
            args.Handled = true;
        };

        // 阻止鼠标滚轮事件, 避免触发鼠标滚轮缩放画布
        PointerWheelChanged += (_, args) =>
        {
            args.Handled = true;
        };

        // 设置拖放事件
        AddHandler(DragDrop.DropEvent, FocusIcon_OnDrop);

        CompletionRewardEditor.TextChanged += OnCompletionRewardTextChanged;

        SizeChanged += (_, args) =>
        {
            if (args.NewSize.Width > 1)
            {
                _settingsService.FocusInfoCardWidth = args.NewSize.Width;
            }
            if (args.NewSize.Height > 1)
            {
                _settingsService.FocusInfoCardHeight = args.NewSize.Height;
            }
        };
    }

    private FocusTreeEditorView? _focusTreeEditorView;

    public void Initialize(FocusTreeEditorView focusTreeEditorView)
    {
        _focusTreeEditorView = focusTreeEditorView;
    }

    // 通过前端绑定
    // ReSharper disable once UnusedMember.Local
    private void AddMutuallyExclusiveDragHandler(object? sender, DragEventArgs e)
    {
        e.Handled = true;
        string? focusId = e.DataTransfer.TryGetText();

        if (focusId is null || _focusTreeEditorView is null || focusNode is null)
        {
            Debug.Assert(false, "数据错误或未初始化");
            return;
        }

        if (!_focusTreeEditorView.ViewModel.TryGetFocus(focusId, out var targetNode))
        {
            return;
        }

        _focusTreeEditorView.ViewModel.CreateConnection(
            focusNode,
            targetNode,
            ConnectionType.MutuallyExclusive
        );

        ResetFocusNodeSelection();
    }

    private void RemoveMutuallyExclusive(object? sender, RoutedEventArgs e)
    {
        if (sender is not Control { DataContext: FocusNode node })
        {
            return;
        }

        Debug.Assert(focusNode is not null);
        focusNode?.RemoveMutuallyExclusive(node);

        StrongReferenceMessenger.Default.Send(RedrawFocusConnectionLinesMessage.Instance);
    }

    // 通过前端绑定
    // ReSharper disable once UnusedMember.Local
    private void AddPrerequisiteDragHandler(object? sender, DragEventArgs e)
    {
        e.Handled = true;
        string? focusId = e.DataTransfer.TryGetText();
        if (focusId is null || _focusTreeEditorView is null || focusNode is null)
        {
            Debug.Assert(false, "数据错误或未初始化");
            return;
        }

        if (!_focusTreeEditorView.ViewModel.TryGetFocus(focusId, out var targetNode))
        {
            return;
        }

        if (sender is Border { DataContext: IReadOnlyList<FocusNode> targetGroup })
        {
            // 在父集合中查找该数据对象的索引
            int index = FindTargetPrerequisiteGroupIndex(targetGroup);

            if (index == -1)
            {
                return;
            }

            Log.Debug("Drop target group index: {Index}", index);

            _focusTreeEditorView.ViewModel.CreateConnection(
                focusNode,
                targetNode,
                ConnectionType.Prerequisite,
                index
            );
        }
        else if (sender is Expander)
        {
            _focusTreeEditorView.ViewModel.CreateConnection(
                focusNode,
                targetNode,
                ConnectionType.Prerequisite
            );
        }

        ResetFocusNodeSelection();
    }

    private int FindTargetPrerequisiteGroupIndex(IReadOnlyList<FocusNode> targetGroup)
    {
        Debug.Assert(focusNode is not null);

        var allGroups = focusNode.Prerequisite;
        int index = -1;

        for (int i = 0; i < allGroups.Count; i++)
        {
            // 使用 ReferenceEquals 确保是同一个 List 对象
            if (ReferenceEquals(allGroups[i], targetGroup))
            {
                index = i;
                break;
            }
        }

        return index;
    }

    private void RemovePrerequisite(object? sender, RoutedEventArgs e)
    {
        if (sender is not Control { DataContext: FocusNode node })
        {
            return;
        }

        Debug.Assert(focusNode is not null);
        focusNode?.RemovePrerequisite(node);
    }

    /// <summary>
    /// 在连接关系建立后, 重置 FocusNode 的选中状态
    /// </summary>
    private void ResetFocusNodeSelection()
    {
        Debug.Assert(_focusTreeEditorView is not null);
        Debug.Assert(focusNode is not null);

        if (
            _focusTreeEditorView.ViewModel.TryGetFocusNodeViewModel(focusNode.Id, out var targetNodeViewModel)
        )
        {
            _focusTreeEditorView.ViewModel.ClearSelection();
            targetNodeViewModel.IsSelected = true;
        }
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
            SetImage(_imageService.GetFocusIconByName(newViewModel.FocusNode.Icon));
        }
    }

    private void OnCompletionRewardTextChanged(object? sender, EventArgs e)
    {
        if (_viewModel is not null)
        {
            _viewModel.FocusNode.CompletionReward = CompletionRewardEditor.Text;
        }
    }

    private void FocusNodeOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(FocusNode.Icon))
        {
            var focusNode = (FocusNode?)sender;
            SetImage(_imageService.GetFocusIconByName(focusNode!.Icon));
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
        string? filePath = files.FirstOrDefault()?.Path.LocalPath;
        if (string.IsNullOrEmpty(filePath))
        {
            return;
        }

        if (ImageHelper.IsValidFocusImageFormat(filePath))
        {
            var result = _fileResourceService.RegisterFocusIcon(filePath);
            if (string.IsNullOrEmpty(result.SpriteName) || string.IsNullOrEmpty(result.DestFilePath))
            {
                _notificationService.Show("添加图标失败");
                return;
            }

            SetImage(_imageService.GetImageSource(result.SpriteName, result.DestFilePath));
            if (_viewModel is not null)
            {
                _viewModel.FocusNode.Icon = result.SpriteName;
            }

            Log.Info("添加图标成功: {Name}", result.SpriteName);
            _notificationService.Show(result.IsConvertToDds ? "添加图标成功, 图片已自动转换为 DDS 格式" : "添加图标成功");
        }
    }

    private void SetImage(Bitmap? bitmap)
    {
        FocusIcon.Source = bitmap;
        FocusIcon.Width = bitmap?.PixelSize.Width ?? 0;
        FocusIcon.Height = bitmap?.PixelSize.Height ?? 0;
    }

    private void ResizeThumbLeft_OnDragDelta(object? sender, VectorEventArgs e)
    {
        double currentWidth = !double.IsNaN(Width) && Width > 0 ? Width : Bounds.Width;
        double newWidth = currentWidth - e.Vector.X;
        Width = Math.Max(MinCardWidth, newWidth);
    }

    private void ResizeThumbBottom_OnDragDelta(object? sender, VectorEventArgs e)
    {
        double currentHeight = !double.IsNaN(Height) && Height > 0 ? Height : Bounds.Height;
        double newHeight = currentHeight + e.Vector.Y;
        Height = Math.Max(MinCardHeight, newHeight);
    }

    private void ResizeThumbBottomLeft_OnDragDelta(object? sender, VectorEventArgs e)
    {
        ResizeThumbLeft_OnDragDelta(sender, e);
        ResizeThumbBottom_OnDragDelta(sender, e);
    }
}
