using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Hoi4BlueprintBuilder.Core.Helpers;
using Hoi4BlueprintBuilder.Core.Infrastructure;
using Hoi4BlueprintBuilder.Core.Messages;
using Hoi4BlueprintBuilder.Core.Models;
using Hoi4BlueprintBuilder.Core.Services.GameResources;
using MessagePipe;
using Microsoft.Extensions.Caching.Memory;
using NLog;
using Pfim;

namespace Hoi4BlueprintBuilder.Core.Services;

[RegisterSingleton<ImageService>]
public sealed class ImageService : IDisposable
{
    private readonly MemoryCache _ddsCache = new(new MemoryCacheOptions());
    private readonly SpriteService _spriteService;
    private readonly GameResourcesPathService _pathService;
    private readonly FileSystemSafeWatcher _fileSystemWatcher;
    private readonly IPublisher<DeleteImageResourceMessage> _deleteImageResourcePublisher;

    private const string Unknown = "GFX_goal_unknown";
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public ImageService(
        SpriteService spriteService,
        SettingsService settingsService,
        GameResourcesPathService pathService,
        IPublisher<DeleteImageResourceMessage> deleteImageResourcePublisher
    )
    {
        _spriteService = spriteService;
        _pathService = pathService;
        _deleteImageResourcePublisher = deleteImageResourcePublisher;
        string path = Path.Combine(settingsService.ModRootFolderPath, "gfx");
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        _fileSystemWatcher = new FileSystemSafeWatcher(path, "*.dds");
        _fileSystemWatcher.Deleted += OnDeleted;
        _fileSystemWatcher.EnableRaisingEvents = true;
        _fileSystemWatcher.IncludeSubdirectories = true;
    }

    private void OnDeleted(object _, FileSystemEventArgs e)
    {
        if (!_ddsCache.TryGetValue<DdsMeta>(e.FullPath, out var meta))
        {
            return;
        }

        _deleteImageResourcePublisher.Publish(new DeleteImageResourceMessage(meta!.SpriteName));
        _ddsCache.Remove(e.FullPath);
    }

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
    /// 获取 <see cref="Bitmap"/> 图像
    /// </summary>
    /// <param name="spriteName">精灵名称</param>
    /// <param name="frame">图片帧数</param>
    /// <returns></returns>
    public Bitmap? GetIconByName(string spriteName, short frame = 1)
    {
        if (!_spriteService.TryGetSpriteInfo(spriteName, out var info))
        {
            return null;
        }

        string? filePath = _pathService.GetFilePathPriorModByRelativePath(info.RelativePath);
        if (filePath is null || !File.Exists(filePath) || frame > info.TotalFrames)
        {
            return null;
        }

        return GetImageSource(spriteName, filePath, frame, info.TotalFrames);
    }

    /// <summary>
    /// 从指定路径加载图像并返回对应的 BitmapSource.
    /// </summary>
    /// <remarks>仅支持 Png 和 Dds 格式</remarks>
    /// <param name="spriteId">图像ID</param>
    /// <param name="filePath">图像文件路径</param>
    /// <returns>如果是不支持的图像格式, 返回 <c>null</c></returns>
    public Bitmap? GetImageSource(string spriteId, string filePath, short frame = 1, short totalFrames = 1)
    {
        try
        {
            var format = ImageHelper.GetImageFormat(filePath);

            Bitmap? bitmap = null;
            if (format == ImageFormatType.Png)
            {
                bitmap = new Bitmap(filePath);
            }
            else if (format == ImageFormatType.Dds)
            {
                bitmap = GetImageSourceFromDds(spriteId, filePath, frame, totalFrames);
            }
            else
            {
                Log.Warn("Unknown image format: {FilePath}", filePath);
            }

            return bitmap;
        }
        catch (Exception e)
        {
            Log.Error(e, "加载图像失败: {FilePath}, spriteId: {SpriteId}", filePath, spriteId);
            return null;
        }
    }

    private Bitmap GetImageSourceFromDds(
        string spriteName,
        string filePath,
        short frame = 1,
        short totalFrames = 1
    )
    {
        var meta = _ddsCache.GetOrCreate(
            filePath,
            entry =>
            {
                using var image = Pfimage.FromFile(filePath);
                var meta = new DdsMeta(
                    spriteName,
                    image.Data,
                    image.Width,
                    image.Height,
                    GetPixelFormat(image),
                    image.Stride
                );
                entry.Value = meta;
                entry.Size = meta.Data.Length;
                entry.SlidingExpiration = TimeSpan.FromMinutes(10);
                return meta;
            }
        );

        int effectiveTotalFrames = totalFrames > 0 ? totalFrames : 1;
        int effectiveFrame = Math.Clamp(frame, (short)1, (short)effectiveTotalFrames);
        int frameWidth = meta!.Width / effectiveTotalFrames;
        int bytesPerPixel = meta.Stride / meta.Width;
        int byteOffset = (effectiveFrame - 1) * frameWidth * bytesPerPixel;

        var handle = GCHandle.Alloc(meta.Data, GCHandleType.Pinned);
        try
        {
            IntPtr addr = handle.AddrOfPinnedObject() + byteOffset;
            return new Bitmap(
                meta.Format,
                AlphaFormat.Unpremul,
                addr,
                new PixelSize(frameWidth, meta.Height),
                new Vector(96, 96),
                meta.Stride
            );
        }
        finally
        {
            handle.Free();
        }
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
            _ => throw new NotSupportedException($"Unable to convert {image.Format} to Avalonia PixelFormat")
        };
    }

    private sealed record DdsMeta(
        string SpriteName,
        byte[] Data,
        int Width,
        int Height,
        PixelFormat Format,
        int Stride
    );

    public void Dispose()
    {
        _fileSystemWatcher.Dispose();
        _ddsCache.Dispose();
    }
}
