namespace Hoi4BlueprintEditor.Models;

public sealed class RegisterFocusIconResult(string iconId, string destFilePath, bool isConvertToDds)
{
    /// <summary>
    /// 国策图标的ID
    /// </summary>
    public string IconId { get; } = iconId;
    /// <summary>
    /// 导入的图标文件的最终路径
    /// </summary>
    public string DestFilePath { get; } = destFilePath;
    /// <summary>
    /// 图片是否转换为了 DDS 格式
    /// </summary>
    public bool IsConvertToDds { get; } = isConvertToDds;
}
