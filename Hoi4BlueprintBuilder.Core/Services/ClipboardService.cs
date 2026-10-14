using Avalonia.Input.Platform;
using Hoi4BlueprintBuilder.Core.Helpers;
using NLog;

namespace Hoi4BlueprintBuilder.Core.Services;

[RegisterSingleton<ClipboardService>]
public sealed class ClipboardService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public Task SetTextAsync(string text)
    {
        if (WindowHelper.TryGetTopLevel() is not { } topLevel || topLevel.Clipboard is null)
        {
            Log.Warn("无法复制文件路径，剪切板不可用");
            return Task.CompletedTask;
        }

        return topLevel.Clipboard.SetTextAsync(text);
    }
}
