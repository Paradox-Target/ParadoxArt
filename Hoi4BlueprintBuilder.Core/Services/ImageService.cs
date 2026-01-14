using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.Messaging;
using Hoi4BlueprintBuilder.Core.Infrastructure;
using Hoi4BlueprintBuilder.Core.Messages;
using Hoi4BlueprintBuilder.Core.Services.GameResources;
using NLog;
using Pfim;

namespace Hoi4BlueprintBuilder.Core.Services;

[RegisterSingleton<ImageService>]
public sealed class ImageService : IDisposable
{
    // TODO: 文件变更监控，变更时释放缓存
    private readonly Dictionary<string, DdsMeta> _ddsHandles = [];
    private readonly SpriteService _spriteService;
    private readonly FileSystemSafeWatcher _fileSystemWatcher;

    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public ImageService(SpriteService spriteService, SettingsService settingsService)
    {
        _spriteService = spriteService;
        _fileSystemWatcher = new FileSystemSafeWatcher(
            Path.Combine(settingsService.ModRootFolderPath, "gfx"),
            "*.dds"
        );
        _fileSystemWatcher.Deleted += OnDeleted;
        _fileSystemWatcher.EnableRaisingEvents = true;
        _fileSystemWatcher.IncludeSubdirectories = true;
    }

    private void OnDeleted(object _, FileSystemEventArgs e)
    {
        if (!_ddsHandles.TryGetValue(e.FullPath, out var meta))
        {
            return;
        }

        meta.Handle.Free();
        _ddsHandles.Remove(e.FullPath);
        StrongReferenceMessenger.Default.Send(new DeleteImageResourceMessage(meta.SpriteName));
    }

    private const string Unknown = "GFX_goal_unknown";

    public Bitmap? GetFocusIconByName(string spriteName)
    {
        if (!_spriteService.TryGetSpriteFilePath(spriteName, out string? filePath))
        {
            _ = _spriteService.TryGetSpriteFilePath(Unknown, out filePath);
        }

        if (filePath is null)
        {
            return null;
        }

        return GetImageSource(spriteName, filePath);
    }

    /// <summary>
    /// 从指定路径加载图像并返回对应的 BitmapSource.
    /// </summary>
    /// <remarks>仅支持 Png 和 Dds 格式</remarks>
    /// <param name="spriteId">图像ID</param>
    /// <param name="filePath">图像文件路径</param>
    /// <returns>如果是不支持的图像格式, 返回 <c>null</c></returns>
    public Bitmap? GetImageSource(string spriteId, string filePath)
    {
        try
        {
            var extension = Path.GetExtension(filePath.AsSpan());
            Bitmap? bitmap = null;
            if (extension.Equals(".png", StringComparison.OrdinalIgnoreCase))
            {
                bitmap = new Bitmap(filePath);
            }

            if (extension.Equals(".dds", StringComparison.OrdinalIgnoreCase))
            {
                bitmap = GetImageSourceFromDds(spriteId, filePath);
            }

            return bitmap;
        }
        catch (Exception e)
        {
            Log.Error(e, "加载图像失败: {FilePath}, spriteId: {SpriteId}", filePath, spriteId);
            return null;
        }
    }

    private Bitmap GetImageSourceFromDds(string spriteName, string filePath)
    {
        if (!_ddsHandles.TryGetValue(filePath, out var meta))
        {
            using var image = Pfimage.FromFile(filePath);
            var pinnedArray = GCHandle.Alloc(image.Data, GCHandleType.Pinned);
            meta = new DdsMeta(
                spriteName,
                pinnedArray,
                image.Width,
                image.Height,
                GetPixelFormat(image),
                image.Stride
            );
            _ddsHandles.Add(filePath, meta);
        }

        IntPtr addr = meta.Handle.AddrOfPinnedObject();
        var bsource = new Bitmap(
            meta.Format,
            AlphaFormat.Unpremul,
            addr,
            new PixelSize(meta.Width, meta.Height),
            new Vector(96, 96),
            meta.Stride
        );

        return bsource;
    }

    private static PixelFormat GetPixelFormat(IImage image)
    {
        return image.Format switch
        {
            ImageFormat.Rgb24 => PixelFormats.Bgr24,
            ImageFormat.Rgba32 => PixelFormats.Bgra8888,
            ImageFormat.Rgb8 => PixelFormats.Gray8,
            ImageFormat.R5g5b5a1 or ImageFormat.R5g5b5 => PixelFormats.Bgr555,
            ImageFormat.R5g6b5 => PixelFormats.Bgr565,
            _ => throw new NotSupportedException($"Unable to convert {image.Format} to WPF PixelFormat")
        };
    }

    private sealed record DdsMeta(
        string SpriteName,
        GCHandle Handle,
        int Width,
        int Height,
        PixelFormat Format,
        int Stride
    );

    public void Dispose()
    {
        _fileSystemWatcher.Dispose();
    }
}
