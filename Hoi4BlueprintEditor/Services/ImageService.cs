using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Pfim;

namespace Hoi4BlueprintEditor.Services;

[RegisterSingleton<ImageService>]
public sealed class ImageService
{
    // TODO: 使用 WeakReference 缓存 ImageMeta?
    private readonly Dictionary<string, ImageMeta> _handles = [];

    public BitmapSource GetImageSource(string filePath)
    {
        // TODO: 支持 Png, 导入 png时转为dds
        // TODO: 文件变更监控，变更时释放缓存
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
