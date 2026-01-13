using ByteSizeLib;
using CommunityToolkit.Mvvm.ComponentModel;
using Hoi4BlueprintBuilder.Core.Helpers;
using Hoi4BlueprintBuilder.Core.Services;
using Hoi4BlueprintBuilder.Core.Views;
using UtfUnknown;

namespace Hoi4BlueprintBuilder.Core.ViewsModels;

[RegisterSingleton<StatusBarViewModel>]
public sealed partial class StatusBarViewModel : ObservableObject
{
    [ObservableProperty]
    private string _focusCountText = string.Empty;

    [ObservableProperty]
    private bool _isVisibleFocusCountText;

    [ObservableProperty]
    private string _ramUsage = "RAM: 0 MB";

    [ObservableProperty]
    private string _textEncoding = string.Empty;

    public StatusBarViewModel(StatusBarService statusBarService, TabViewService tabViewService)
    {
        statusBarService.UpdateFocusCount += count =>
        {
            FocusCountText = $"国策总数: {count}";
        };
        statusBarService.UpdateRamBytesUsage += ram =>
        {
            double mb = ByteSize.FromBytes(ram).MebiBytes;
            RamUsage = $"内存使用: {mb:F1} MB";
        };

        tabViewService.CurrentItemChanged += currentItem =>
        {
            IsVisibleFocusCountText = currentItem is FocusTreeEditorView;
            Task.Run(() => DetectEncodingAsync(currentItem.FilePath));
        };
    }

    private async Task DetectEncodingAsync(string filePath)
    {
        if (File.Exists(filePath) && FileCheckHelper.IsTextFile(filePath))
        {
            var result = await CharsetDetector.DetectFromFileAsync(filePath);
            if (result.Detected is null)
            {
                TextEncoding = "Unknown";
            }
            else
            {
                string encodingName = result.Detected.EncodingName;
                // 将 ASCII 视为 UTF-8
                if (encodingName.Equals("ascii", StringComparison.OrdinalIgnoreCase))
                {
                    encodingName = "UTF-8";
                }
                TextEncoding = result.Detected.HasBOM
                    ? $"{encodingName.ToUpper()}-BOM"
                    : $"{encodingName.ToUpper()}";
            }
        }
        else
        {
            TextEncoding = string.Empty;
        }
    }
}
