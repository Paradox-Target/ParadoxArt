using Avalonia.Media.Imaging;
using Hoi4BlueprintBuilder.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Hoi4BlueprintBuilder.Core.Models.Focus;

public sealed class FocusIcon
{
    public string Name { get; }

    public Bitmap? Icon => _icon.Value;
    private readonly Lazy<Bitmap?> _icon;

    public FocusIcon(string name)
    {
        Name = name;
        _icon = new Lazy<Bitmap?>(() => ImageService.GetFocusIconByName(Name));
    }

    private static readonly ImageService ImageService =
        App.Current.Services.GetRequiredService<ImageService>();
}
