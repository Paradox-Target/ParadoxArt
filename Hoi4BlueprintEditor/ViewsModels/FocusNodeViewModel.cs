using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using Hoi4BlueprintEditor.Models.Focus;
using Hoi4BlueprintEditor.Services;
using Hoi4BlueprintEditor.Services.GameResources;
using Hoi4BlueprintEditor.Services.GameResources.Localization;
using Microsoft.Extensions.DependencyInjection;

namespace Hoi4BlueprintEditor.ViewsModels;

public sealed class FocusNodeViewModel : ObservableObject
{
    public FocusNodeViewModel(FocusNode model)
    {
        Model = model;
        BitmapSource = GetBitmapSource();
        Width = BitmapSource?.Width ?? 64;
        Height = BitmapSource?.Height ?? 64;
    }

    public FocusNode Model { get; }
    public string LocalizedName => LocalizationService.GetFormatText(Model.Id);

    public BitmapSource? BitmapSource { get; }
    public double Width { get; }
    public double Height { get; }

    private static readonly LocalizationFormatService LocalizationService =
        App.Current.Services.GetRequiredService<LocalizationFormatService>();
    private static readonly ImageService ImageService =
        App.Current.Services.GetRequiredService<ImageService>();
    private static readonly SpriteService SpriteService =
        App.Current.Services.GetRequiredService<SpriteService>();

    private BitmapSource? GetBitmapSource()
    {
        if (!SpriteService.TryGetSpriteFilePath(Model.Icon, out string? filePath))
        {
            return null;
        }

        return ImageService.GetImageSource(filePath);
    }
}
