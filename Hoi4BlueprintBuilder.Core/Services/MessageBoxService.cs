using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

namespace Hoi4BlueprintBuilder.Core.Services;

[RegisterSingleton<MessageBoxService>]
public sealed class MessageBoxService
{
    public Task ShowAsync(string message, string title = "", MessageBoxIcon icon = MessageBoxIcon.Info)
    {
        return Dispatcher.UIThread.Invoke(() =>
        {
            var box = MessageBoxManager.GetMessageBoxStandard(title, message, icon: ToMsBoxIcon(icon));

            var lifetime = App.Current.ApplicationLifetime;
            if (lifetime is ISingleViewApplicationLifetime)
            {
                return box.ShowAsync();
            }

            if (lifetime is IClassicDesktopStyleApplicationLifetime { MainWindow: not null } desktop)
            {
                return box.ShowWindowDialogAsync(desktop.MainWindow);
            }

            return box.ShowAsync();
        });
    }

    private static Icon ToMsBoxIcon(MessageBoxIcon icon)
    {
        return icon switch
        {
            MessageBoxIcon.Info => Icon.Info,
            MessageBoxIcon.Warning => Icon.Warning,
            MessageBoxIcon.Error => Icon.Error,
            _ => Icon.Info
        };
    }
}

public enum MessageBoxIcon : byte
{
    Info,
    Warning,
    Error
}
