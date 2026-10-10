namespace Hoi4BlueprintBuilder.Core.Services;

public interface IAndroidFileService
{
    /// <summary>
    /// 尝试把选择器返回的存储 URI (content:// 或 file://) 转换为本地绝对路径。
    /// </summary>
    string? TryGetLocalPath(Uri uri);
}