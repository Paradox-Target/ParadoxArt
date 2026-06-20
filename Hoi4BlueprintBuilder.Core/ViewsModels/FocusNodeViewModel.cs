using System.ComponentModel;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using Hoi4BlueprintBuilder.Core.Models.Focus;
using Hoi4BlueprintBuilder.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Hoi4BlueprintBuilder.Core.ViewsModels;

public sealed partial class FocusNodeViewModel : ObservableObject, IDisposable
{
    public double ActualX => Node.X * FocusCellWidth;
    public double ActualY => Node.Y * FocusCellHeight;

    public FocusNode Node { get; }

    [ObservableProperty]
    public partial Bitmap? Bitmap { get; set; }

    [ObservableProperty]
    public partial double Width { get; set; }

    [ObservableProperty]
    public partial double Height { get; set; }

    /// <summary>
    /// 是否被选中
    /// </summary>
    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    /// <summary>
    /// 该 Focus 是否已完成 (用于条件求值)
    /// </summary>
    [ObservableProperty]
    public partial bool IsCompleted { get; set; }

    /// <summary>
    /// 是否显示完成 CheckBox (仅当 has_completed_focus 条件引用了此 Focus 时)
    /// </summary>
    [ObservableProperty]
    public partial bool ShowCompletedCheckbox { get; set; }

    public double FocusCellWidth => ProjectConfigService.FocusCellWidth;
    public double FocusCellHeight => ProjectConfigService.FocusCellHeight;
    public double FocusHeight => ProjectConfigService.FocusHeight;
    public double FocusNameUpOffset => ProjectConfigService.FocusNameUpOffset;

    private static readonly ImageService ImageService =
        App.Current.Services.GetRequiredService<ImageService>();
    private static readonly ProjectConfigService ProjectConfigService =
        App.Current.Services.GetRequiredService<ProjectConfigService>();

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
        else if (args.PropertyName == nameof(Node.X))
        {
            OnPropertyChanged(nameof(ActualX));
        }
        else if (args.PropertyName == nameof(Node.Y))
        {
            OnPropertyChanged(nameof(ActualY));
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
