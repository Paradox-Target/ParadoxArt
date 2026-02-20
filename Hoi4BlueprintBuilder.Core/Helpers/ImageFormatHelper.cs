using Hoi4BlueprintBuilder.Core.Models;
using NLog;

namespace Hoi4BlueprintBuilder.Core.Helpers;

public static class ImageFormatHelper
{
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly byte[] DdsSignature = "DDS "u8.ToArray();

    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// 读取文件头判断图像格式 (支持 PNG, DDS)
    /// </summary>
    /// <exception cref="FileNotFoundException">文件不存在时</exception>
    public static ImageFormatType GetImageFormat(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Image File not found: {filePath}");
        }

        try
        {
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);

            // 读取前 8 个字节
            Span<byte> header = stackalloc byte[8];
            int bytesRead = fs.Read(header);

            if (bytesRead < 4)
            {
                return ImageFormatType.Unknown;
            }

            if (StartsWith(header, DdsSignature))
            {
                return ImageFormatType.Dds;
            }

            if (StartsWith(header, PngSignature))
            {
                return ImageFormatType.Png;
            }

            return ImageFormatType.Unknown;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "读取图像文件失败: {FilePath}", filePath);
            return ImageFormatType.Unknown;
        }
    }

    private static bool StartsWith(Span<byte> data, byte[] signature)
    {
        if (data.Length < signature.Length)
        {
            return false;
        }

        for (int i = 0; i < signature.Length; i++)
        {
            if (data[i] != signature[i])
            {
                return false;
            }
        }
        return true;
    }
}
