using System.IO;

namespace Hoi4BlueprintEditor.Helpers;

public static class ImageHelper
{
    /// <summary>
    /// 检查是否是 HOI4 支持的国策图像格式
    /// </summary>
    /// <param name="filePathOrFileName"></param>
    /// <returns></returns>
    public static bool IsValidFocusImageFormat(string filePathOrFileName)
    {
        var extension = Path.GetExtension(filePathOrFileName.AsSpan());
        return extension.Equals(".png", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".dds", StringComparison.OrdinalIgnoreCase);
    }
}
