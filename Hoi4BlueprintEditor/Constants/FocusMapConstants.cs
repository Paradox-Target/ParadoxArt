namespace Hoi4BlueprintEditor.Constants;

public static class FocusMapConstants
{
    public const double CellWidth = 96;
    public const double CellHeight = 130;
    /// <summary>
    /// 国策在单元格中的大小占比
    /// </summary>
    public const double FocusFactor = 0.8;
    public const double FocusWidth = CellWidth;
    public const double FocusHeight = CellHeight * FocusFactor;
    /// <summary>
    /// 国策竖直方向居中偏移
    /// </summary>
    public const double FocusCenterOffsetVertical = (CellHeight - FocusHeight) / 2.0;
    /// <summary>
    /// 国策名称和图标向上偏移以使二者整体居中
    /// </summary>
    public const double FocusContentUpOffset = -FocusCenterOffsetVertical / 2.0;
}
