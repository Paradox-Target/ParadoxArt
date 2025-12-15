using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Hoi4BlueprintBuilder.Core.Models.Focus;
using Hoi4BlueprintBuilder.Core.Services;
using Hoi4BlueprintBuilder.Core.Services.GameResources.Localization;
using Microsoft.Extensions.DependencyInjection;

namespace Hoi4BlueprintBuilder.Core.ViewModels;

public sealed partial class FocusNodeViewModel : ObservableObject, IDisposable
{
    public FocusNode Model { get; }
    public string LocalizedName => LocalizationFormatService.GetFormatText(Model.Id);

    [ObservableProperty]
    private BitmapSource? _bitmapSource;

    [ObservableProperty]
    private double _width;

    [ObservableProperty]
    private double _height;

    private static readonly LocalizationFormatService LocalizationFormatService =
        App.Current.Services.GetRequiredService<LocalizationFormatService>();
    private static readonly ImageService ImageService =
        App.Current.Services.GetRequiredService<ImageService>();

    public FocusNodeViewModel(FocusNode model)
    {
        Model = model;
        BitmapSource = ImageService.GetFocusIconByName(Model.Icon);
        Width = BitmapSource?.PixelWidth ?? 0;
        Height = BitmapSource?.PixelHeight ?? 0;

        Model.PropertyChanged += OnModelPropertyChanged;
    }

    private void OnModelPropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName == nameof(Model.Id))
        {
            OnPropertyChanged(nameof(LocalizedName));
        }
        else if (args.PropertyName == nameof(Model.Icon))
        {
            BitmapSource = ImageService.GetFocusIconByName(Model.Icon);
            Width = BitmapSource?.PixelWidth ?? 0;
            Height = BitmapSource?.PixelHeight ?? 0;
        }
    }

    /// <summary>
    /// 取消事件订阅, 清理所属 <see cref="FocusNode"/> 所有的连接关系
    /// </summary>
    public void Dispose()
    {
        Model.PropertyChanged -= OnModelPropertyChanged;
        Model.Dispose();
    }
}
