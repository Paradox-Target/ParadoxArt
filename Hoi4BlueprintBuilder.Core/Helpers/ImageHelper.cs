using Hoi4BlueprintBuilder.Core.Models;

namespace Hoi4BlueprintBuilder.Core.Helpers;

public static class ImageHelper
{
    /// <summary>
    /// 检查是否是 HOI4 支持的国策图像格式
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns></returns>
    public static bool IsValidFocusImageFormat(string filePath)
    {
        return ImageFormatHelper.GetImageFormat(filePath) is ImageFormatType.Png or ImageFormatType.Dds;
    }
}
