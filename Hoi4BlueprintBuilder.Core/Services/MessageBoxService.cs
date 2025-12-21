using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

namespace Hoi4BlueprintBuilder.Core.Services;

[RegisterSingleton<MessageBoxService>]
public sealed class MessageBoxService
{
    public Task<MessageBoxResult> ShowAsync(
        string message,
        string title = "",
        MessageBoxIcon icon = MessageBoxIcon.Info,
        MessageBoxButtons buttons = MessageBoxButtons.Ok
    )
    {
        return Dispatcher.UIThread.Invoke(() =>
        {
            var buttonType = ToMsBoxButtons(buttons);
            var box = MessageBoxManager.GetMessageBoxStandard(
                title,
                message,
                @enum: buttonType,
                icon: ToMsBoxIcon(icon)
            );

            var lifetime = App.Current.ApplicationLifetime;
            Task<MessageBoxResult> showTask;

            if (lifetime is ISingleViewApplicationLifetime)
            {
                showTask = box.ShowAsync().ContinueWith(t => GetButtonResult(t.Result));
            }
            else if (lifetime is IClassicDesktopStyleApplicationLifetime { MainWindow: not null } desktop)
            {
                showTask = box.ShowWindowDialogAsync(desktop.MainWindow)
                    .ContinueWith(t => GetButtonResult(t.Result));
            }
            else
            {
                showTask = box.ShowAsync().ContinueWith(t => GetButtonResult(t.Result));
            }

            return showTask;
        });
    }

    private static MessageBoxResult GetButtonResult(ButtonResult result)
    {
        return result switch
        {
            ButtonResult.Ok => MessageBoxResult.Ok,
            ButtonResult.Yes => MessageBoxResult.Yes,
            ButtonResult.No => MessageBoxResult.No,
            _ => MessageBoxResult.Ok
        };
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

    private static ButtonEnum ToMsBoxButtons(MessageBoxButtons buttons)
    {
        return buttons switch
        {
            MessageBoxButtons.Ok => ButtonEnum.Ok,
            MessageBoxButtons.YesNo => ButtonEnum.YesNo,
            _ => ButtonEnum.Ok
        };
    }
}

public enum MessageBoxIcon : byte
{
    Info,
    Warning,
    Error
}

public enum MessageBoxResult : byte
{
    Ok,
    Yes,
    No
}

public enum MessageBoxButtons : byte
{
    Ok,
    YesNo
}
