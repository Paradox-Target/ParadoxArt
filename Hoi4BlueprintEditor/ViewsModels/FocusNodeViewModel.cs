using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using Hoi4BlueprintEditor.Models.Focus;
using Hoi4BlueprintEditor.Services;
using Hoi4BlueprintEditor.Services.GameResources.Localization;
using Microsoft.Extensions.DependencyInjection;

namespace Hoi4BlueprintEditor.ViewsModels;

public sealed class FocusNodeViewModel : ObservableObject
{
    public FocusNode Model { get; }
    public string LocalizedName => LocalizationService.GetFormatText(Model.Id);
    public BitmapSource? BitmapSource { get; }
    public double Width { get; }
    public double Height { get; }

    private static readonly LocalizationFormatService LocalizationService =
        App.Current.Services.GetRequiredService<LocalizationFormatService>();
    private static readonly ImageService ImageService =
        App.Current.Services.GetRequiredService<ImageService>();

    public FocusNodeViewModel(FocusNode model)
    {
        Model = model;
        BitmapSource = ImageService.GetFocusIconByName(Model.Icon);
        Width = BitmapSource?.PixelWidth ?? 0;
        Height = BitmapSource?.PixelHeight ?? 0;
        Model.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(Model.Id))
            {
                OnPropertyChanged(nameof(LocalizedName));
            }
        };
    }
}
