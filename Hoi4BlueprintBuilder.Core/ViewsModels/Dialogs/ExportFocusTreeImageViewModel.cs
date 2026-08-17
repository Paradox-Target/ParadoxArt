using CommunityToolkit.Mvvm.ComponentModel;
using Hoi4BlueprintBuilder.Core.Services;
using Hoi4BlueprintBuilder.Localization.Strings;

namespace Hoi4BlueprintBuilder.Core.ViewsModels.Dialogs;

/// <summary>
/// 导出格式选项
/// </summary>
public sealed record FocusTreeExportFormatOption(FocusTreeExportFormat Format, string DisplayName);

/// <summary>
/// 导出国策截图配置对话框的视图模型
/// </summary>
public sealed partial class ExportFocusTreeImageViewModel : ObservableObject
{
    private static readonly FocusTreeExportFormatOption PngFormat =
        new(FocusTreeExportFormat.Png, LangResources.ExportFocusTreeImage_FormatPng);

    private static readonly FocusTreeExportFormatOption JpegFormat =
        new(FocusTreeExportFormat.Jpeg, LangResources.ExportFocusTreeImage_FormatJpeg);

    public IReadOnlyList<FocusTreeExportFormatOption> OutputFormatOptions { get; } = [PngFormat, JpegFormat];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsJpegSelected))]
    public partial FocusTreeExportFormatOption SelectedOutputFormat { get; set; } = PngFormat;

    public bool IsJpegSelected => SelectedOutputFormat.Format == FocusTreeExportFormat.Jpeg;

    public string[] FontFamilyOptions { get; } =
        ["Microsoft YaHei", "SimSun", "SimHei", "KaiTi", "Segoe UI", "Arial"];

    [ObservableProperty]
    public partial double Padding { get; set; } = 1.0;

    [ObservableProperty]
    public partial bool ShowNames { get; set; } = true;

    [ObservableProperty]
    public partial double NameFontSize { get; set; } = 13;

    [ObservableProperty]
    public partial string NameFontFamily { get; set; } = "Microsoft YaHei";

    [ObservableProperty]
    public partial bool ShowConnections { get; set; } = true;

    [ObservableProperty]
    public partial int JpegQuality { get; set; } = 95;

    public FocusTreeExportOptions CreateOptions() =>
        new()
        {
            OutputFormat = SelectedOutputFormat.Format,
            Padding = Padding,
            ShowNames = ShowNames,
            NameFontSize = NameFontSize,
            NameFontFamily = NameFontFamily,
            ShowConnections = ShowConnections,
            JpegQuality = JpegQuality
        };
}
