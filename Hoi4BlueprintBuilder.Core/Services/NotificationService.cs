using JetBrains.Annotations;

namespace Hoi4BlueprintBuilder.Core.Services;

[RegisterSingleton<NotificationService>]
public sealed class NotificationService : IDisposable
{
    [LocalizationRequired]
    public void Show(string message)
    {
        new ToastContentBuilder().AddText(message).Show();
    }

    public void Dispose()
    {
        ToastNotificationManagerCompat.Uninstall();
    }
}
