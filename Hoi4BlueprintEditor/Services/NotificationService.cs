using Microsoft.Toolkit.Uwp.Notifications;

namespace Hoi4BlueprintEditor.Services;

[RegisterSingleton<NotificationService>]
public sealed class NotificationService : IDisposable
{
    public void Show(string message)
    {
        new ToastContentBuilder().AddText(message).Show();
    }

    public void Dispose()
    {
        ToastNotificationManagerCompat.Uninstall();
    }
}
