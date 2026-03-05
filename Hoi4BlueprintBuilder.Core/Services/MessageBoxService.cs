using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using JetBrains.Annotations;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

namespace Hoi4BlueprintBuilder.Core.Services;

[RegisterSingleton<MessageBoxService>]
public sealed class MessageBoxService
{
    [LocalizationRequired]
    public Task ShowErrorAsync(string message, string title = "")
    {
        return ShowAsync(message, title, MessageBoxIcon.Error);
    }

    [LocalizationRequired]
    public Task<MessageBoxResult> ShowAsync(
        string message,
        string title = "",
        MessageBoxIcon icon = MessageBoxIcon.Info,
        MessageBoxButtons buttons = MessageBoxButtons.Ok
    )
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            return ShowAsyncCore(message, title, icon, buttons);
        }
        return Dispatcher.UIThread.InvokeAsync(() => ShowAsyncCore(message, title, icon, buttons));
    }

    private async Task<MessageBoxResult> ShowAsyncCore(
        string message,
        string title = "",
        MessageBoxIcon icon = MessageBoxIcon.Info,
        MessageBoxButtons buttons = MessageBoxButtons.Ok
    )
    {
        var buttonType = ToMsBoxButtons(buttons);
        var box = MessageBoxManager.GetMessageBoxStandard(
            title,
            message,
            @enum: buttonType,
            icon: ToMsBoxIcon(icon)
        );

        var lifetime = App.Current.ApplicationLifetime;
        Task<ButtonResult> showTask;

        if (lifetime is ISingleViewApplicationLifetime)
        {
            showTask = box.ShowAsync();
        }
        else if (lifetime is IClassicDesktopStyleApplicationLifetime { MainWindow: not null } desktop)
        {
            showTask = box.ShowWindowDialogAsync(desktop.MainWindow);
        }
        else
        {
            showTask = box.ShowAsync();
        }

        return GetButtonResult(await showTask);
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
