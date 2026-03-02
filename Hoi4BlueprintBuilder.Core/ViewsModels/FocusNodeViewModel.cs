using System.ComponentModel;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using Hoi4BlueprintBuilder.Core.Models.Focus;
using Hoi4BlueprintBuilder.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Hoi4BlueprintBuilder.Core.ViewsModels;

public sealed partial class FocusNodeViewModel : ObservableObject, IDisposable
{
    public FocusNode Node { get; }

    [ObservableProperty]
    private Bitmap? _bitmap;

    [ObservableProperty]
    private double _width;

    [ObservableProperty]
    private double _height;

    /// <summary>
    /// 是否被选中
    /// </summary>
    [ObservableProperty]
    private bool _isSelected;

    /// <summary>
    /// 该 Focus 是否已完成 (用于条件求值)
    /// </summary>
    [ObservableProperty]
    private bool _isCompleted;

    /// <summary>
    /// 是否显示完成 CheckBox (仅当 has_completed_focus 条件引用了此 Focus 时)
    /// </summary>
    [ObservableProperty]
    private bool _showCompletedCheckbox;

    private static readonly ImageService ImageService =
        App.Current.Services.GetRequiredService<ImageService>();

    public FocusNodeViewModel(FocusNode node)
    {
        Node = node;
        LoadBitmapSource();

        Node.PropertyChanged += OnNodePropertyChanged;
    }

    private void OnNodePropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName == nameof(Node.Icon))
        {
            LoadBitmapSource();
        }
    }

    private void LoadBitmapSource()
    {
        Bitmap = ImageService.GetFocusIconByName(Node.Icon);
        Width = Bitmap?.PixelSize.Width ?? 0;
        Height = Bitmap?.PixelSize.Height ?? 0;
    }

    /// <summary>
    /// 取消事件订阅, 清理所属 <see cref="FocusNode"/> 所有的连接关系
    /// </summary>
    public void Dispose()
    {
        Bitmap?.Dispose();
        Node.PropertyChanged -= OnNodePropertyChanged;
        Node.Dispose();
    }
}
