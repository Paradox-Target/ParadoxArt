using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Hoi4BlueprintEditor.Services.GameResources;
using Pfim;

namespace Hoi4BlueprintEditor.Services;

[RegisterSingleton<ImageService>]
public sealed class ImageService(SpriteService spriteService)
{
    // TODO: 导入png时转为dds
    // TODO: 文件变更监控，变更时释放缓存
    private readonly Dictionary<string, ImageMeta> _handles = [];

    private const string Unknown = "GFX_goal_unknown";

    public BitmapSource? GetFocusIconByName(string spriteName)
    {
        if (!spriteService.TryGetSpriteFilePath(spriteName, out string? filePath))
        {
            _ = spriteService.TryGetSpriteFilePath(Unknown, out filePath);
        }

        if (filePath is null)
        {
            return null;
        }

        return GetImageSource(filePath);
    }

    /// <summary>
    /// 从指定路径加载图像并返回对应的 BitmapSource.
    /// </summary>
    /// <remarks>仅支持 Png 和 Dds 格式</remarks>
    /// <param name="filePath">图像文件路径</param>
    /// <returns>如果是不支持的图像格式, 返回 <c>null</c></returns>
    public BitmapSource? GetImageSource(string filePath)
    {
        var extension = Path.GetExtension(filePath.AsSpan());
        BitmapSource? bitmapSource = null;
        if (extension.Equals(".png", StringComparison.OrdinalIgnoreCase))
        {
            bitmapSource = new BitmapImage(new Uri(filePath, UriKind.Absolute));
        }

        if (extension.Equals(".dds", StringComparison.OrdinalIgnoreCase))
        {
            bitmapSource = GetImageSourceFromDds(filePath);
        }

        bitmapSource?.Freeze();
        return bitmapSource;
    }

    private BitmapSource GetImageSourceFromDds(string filePath)
    {
        if (!_handles.TryGetValue(filePath, out var meta))
        {
            using var image = Pfimage.FromFile(filePath);
            var pinnedArray = GCHandle.Alloc(image.Data, GCHandleType.Pinned);
            meta = new ImageMeta(
                pinnedArray,
                image.Width,
                image.Height,
                GetPixelFormat(image),
                image.DataLen,
                image.Stride
            );
            _handles.Add(filePath, meta);
        }

        IntPtr addr = meta.Handle.AddrOfPinnedObject();
        var bsource = BitmapSource.Create(
            meta.Width,
            meta.Height,
            96.0,
            96.0,
            meta.Format,
            null,
            addr,
            meta.DataLength,
            meta.Stride
        );

        return bsource;
    }

    private static PixelFormat GetPixelFormat(IImage image)
    {
        return image.Format switch
        {
            ImageFormat.Rgb24 => PixelFormats.Bgr24,
            ImageFormat.Rgba32 => PixelFormats.Bgra32,
            ImageFormat.Rgb8 => PixelFormats.Gray8,
            ImageFormat.R5g5b5a1 or ImageFormat.R5g5b5 => PixelFormats.Bgr555,
            ImageFormat.R5g6b5 => PixelFormats.Bgr565,
            _ => throw new Exception($"Unable to convert {image.Format} to WPF PixelFormat")
        };
    }

    private sealed record ImageMeta(
        GCHandle Handle,
        int Width,
        int Height,
        PixelFormat Format,
        int DataLength,
        int Stride
    );

    public void Clear()
    {
        foreach (var meta in _handles.Values)
        {
            meta.Handle.Free();
        }
        _handles.Clear();
    }
}
