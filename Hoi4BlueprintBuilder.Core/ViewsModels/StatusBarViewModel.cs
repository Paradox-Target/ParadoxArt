using ByteSizeLib;
using CommunityToolkit.Mvvm.ComponentModel;
using Hoi4BlueprintBuilder.Core.Services;
using Hoi4BlueprintBuilder.Core.Views;

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
        };
    }
}
