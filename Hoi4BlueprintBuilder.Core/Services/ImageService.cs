using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.Messaging;
using Hoi4BlueprintBuilder.Core.Helpers;
using Hoi4BlueprintBuilder.Core.Infrastructure;
using Hoi4BlueprintBuilder.Core.Messages;
using Hoi4BlueprintBuilder.Core.Models;
using Hoi4BlueprintBuilder.Core.Services.GameResources;
using Microsoft.Extensions.Caching.Memory;
using NLog;
using Pfim;

namespace Hoi4BlueprintBuilder.Core.Services;

[RegisterSingleton<ImageService>]
public sealed class ImageService : IDisposable
{
    private readonly MemoryCache _ddsCache = new(new MemoryCacheOptions());
    private readonly SpriteService _spriteService;
    private readonly FileSystemSafeWatcher _fileSystemWatcher;

    private const string Unknown = "GFX_goal_unknown";
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public ImageService(SpriteService spriteService, SettingsService settingsService)
    {
        _spriteService = spriteService;
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

        StrongReferenceMessenger.Default.Send(new DeleteImageResourceMessage(meta!.SpriteName));
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
            var format = ImageFormatHelper.GetImageFormat(filePath);

            Bitmap? bitmap = null;
            if (format == ImageFormatType.Png)
            {
                bitmap = new Bitmap(filePath);
            }
            else if (format == ImageFormatType.Dds)
            {
                bitmap = GetImageSourceFromDds(spriteId, filePath);
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

    private Bitmap GetImageSourceFromDds(string spriteName, string filePath)
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

        var handle = GCHandle.Alloc(meta!.Data, GCHandleType.Pinned);
        try
        {
            IntPtr addr = handle.AddrOfPinnedObject();
            return new Bitmap(
                meta.Format,
                AlphaFormat.Unpremul,
                addr,
                new PixelSize(meta.Width, meta.Height),
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
