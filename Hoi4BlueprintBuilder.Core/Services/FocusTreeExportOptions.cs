namespace Hoi4BlueprintBuilder.Core.Services;

/// <summary>
/// 国策树截图导出的图片格式
/// </summary>
public enum FocusTreeExportFormat : byte
{
    /// <summary>
    /// PNG 格式
    /// </summary>
    Png,

    /// <summary>
    /// JPEG 格式
    /// </summary>
    Jpeg
}

/// <summary>
/// 导出国策截图的配置项
/// </summary>
public sealed record FocusTreeExportOptions
{
    /// <summary>
    /// 输出图片格式
    /// </summary>
    public FocusTreeExportFormat OutputFormat { get; init; } = FocusTreeExportFormat.Png;

    /// <summary>
    /// 图片四周内边距, 以网格为单位
    /// </summary>
    public double Padding { get; init; } = 1.0;

    /// <summary>
    /// 是否绘制国策名称
    /// </summary>
    public bool ShowNames { get; init; } = true;

    /// <summary>
    /// 国策名称字号
    /// </summary>
    public double NameFontSize { get; init; } = 13;

    /// <summary>
    /// 国策名称字体
    /// </summary>
    public string NameFontFamily { get; init; } = "Microsoft YaHei";

    /// <summary>
    /// 是否绘制节点之间的连线
    /// </summary>
    public bool ShowConnections { get; init; } = true;

    /// <summary>
    /// JPEG 图片质量，范围为 0 到 100
    /// </summary>
    public int JpegQuality { get; init; } = 95;
}
