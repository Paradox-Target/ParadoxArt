using Android.Provider;
using Hoi4BlueprintBuilder.Core.Services;
using NLog;
using Environment = Android.OS.Environment;

namespace Hoi4BlueprintBuilder.Androi.Services;

public sealed class AndroidFileService : IAndroidFileService
{
    private const string ExternalStorageAuthority = "com.android.externalstorage.documents";
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public string? TryGetLocalPath(Uri uri)
    {
        try
        {
            var androidUri = Android.Net.Uri.Parse(uri.ToString());
            switch (androidUri?.Scheme)
            {
                case "file":
                    return androidUri.Path;
                case "content":
                    return TryConvertTreeUri(androidUri);
                default:
                    Log.Warn("无法识别的存储 URI 方案 {Uri}", uri);
                    return null;
            }
        }
        catch (Exception e)
        {
            Log.Warn(e, "转换存储 URI {Uri} 为本地路径时发生错误", uri);
            return null;
        }
    }

    private static string? TryConvertTreeUri(Android.Net.Uri uri)
    {
        if (uri.Authority != ExternalStorageAuthority)
        {
            Log.Warn("存储提供方 {Authority} 不支持转换为本地路径", uri.Authority);
            return null;
        }

        // tree URI 的 documentId 形如 "primary:Download/mods" (主存储)
        // 或 "<卷UUID>:mods" (SD 卡等次级存储)
        string? documentId = DocumentsContract.GetTreeDocumentId(uri);
        if (string.IsNullOrEmpty(documentId))
        {
            Log.Warn("无法从存储 URI {Uri} 解析 documentId", uri);
            return null;
        }

        int separator = documentId.IndexOf(':');
        if (separator < 0)
        {
            Log.Warn("无法解析的 documentId {DocumentId}", documentId);
            return null;
        }

        string volumeId = documentId[..separator];
        string relativePath = documentId[(separator + 1)..];

        string? volumeRoot = GetVolumeRoot(volumeId);
        if (volumeRoot is null)
        {
            Log.Warn("无法解析存储卷 {VolumeId} 的挂载目录", volumeId);
            return null;
        }

        // documentId 为 "primary:" 时表示选中了存储根目录
        return relativePath.Length == 0
            ? volumeRoot
            : new Java.IO.File(volumeRoot, relativePath).AbsolutePath;
    }

    private static string? GetVolumeRoot(string volumeId)
    {
        if (volumeId == "primary")
        {
            // /storage/emulated/0
            return Environment.ExternalStorageDirectory?.AbsolutePath;
        }

        // 次级存储卷 (SD 卡 / U 盘) 的挂载点以卷 UUID 命名, 如 /storage/1A2B-3C4D
        var volume = new Java.IO.File("/storage", volumeId);
        return volume.Exists() ? volume.AbsolutePath : null;
    }
}
