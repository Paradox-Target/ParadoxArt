using ByteSizeLib;
using CommunityToolkit.Mvvm.ComponentModel;
using Cysharp.Text;
using Hoi4BlueprintBuilder.Core.Extensions;
using Hoi4BlueprintBuilder.Core.Helpers;
using Hoi4BlueprintBuilder.Core.Services;
using Hoi4BlueprintBuilder.Core.Views;
using Hoi4BlueprintBuilder.Localization.Strings;
using UtfUnknown;

namespace Hoi4BlueprintBuilder.Core.ViewsModels;

[RegisterSingleton<StatusBarViewModel>]
public sealed partial class StatusBarViewModel : ObservableObject
{
    [ObservableProperty]
    public partial string FocusCountText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsVisibleFocusCountText { get; set; }

    [ObservableProperty]
    public partial string RamUsage { get; set; } = "RAM: 0 MB";

    [ObservableProperty]
    public partial string TextEncoding { get; set; } = string.Empty;

    public StatusBarViewModel(StatusBarService statusBarService, TabViewService tabViewService)
    {
        statusBarService.UpdateFocusCount += count =>
        {
            FocusCountText = ZString.Format(LangResources.StatusBar_FocusSum, count);
        };
        statusBarService.UpdateRamBytesUsage += ram =>
        {
            // TODO: humanized?
            double mb = ByteSize.FromBytes(ram).MebiBytes;
            RamUsage = $"{string.Format(LangResources.StatusBar_RAM, mb.ToString("F1"))} MB";
        };

        tabViewService.CurrentItemChanged += currentItem =>
        {
            IsVisibleFocusCountText = currentItem is FocusTreeEditorView;
            Task.Run(() => DetectEncodingAsync(currentItem?.FilePath));
        };
    }

    private async Task DetectEncodingAsync(string? filePath)
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
                if (encodingName.EqualsIgnoreCase("ascii"))
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
