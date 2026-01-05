using Hoi4BlueprintBuilder.Core.Views;
using NLog;

namespace Hoi4BlueprintBuilder.Core.Services;

[RegisterSingleton<ClipboardService>]
public sealed class ClipboardService(MainWindow window)
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public Task SetTextAsync(string text)
    {
        if (window.Clipboard is null)
        {
            Log.Warn("无法复制文件路径，剪切板不可用");
            return Task.CompletedTask;
        }

        return window.Clipboard.SetTextAsync(text);
    }
}
