using System.Diagnostics;
using Avalonia.Controls.Notifications;
using Hoi4BlueprintBuilder.Core.Views;
using JetBrains.Annotations;

namespace Hoi4BlueprintBuilder.Core.Services;

[RegisterSingleton<NotificationService>]
public sealed class NotificationService
{
    private WindowNotificationManager? _notificationManager;

    [LocalizationRequired]
    public void Show(
        string message,
        string title = "",
        NotificationType notificationType = NotificationType.Information
    )
    {
        _notificationManager?.Show(new Notification(title, message, notificationType));
    }

    public void Initialize(MainWindow mainWindow)
    {
        Debug.Assert(_notificationManager is null);

        _notificationManager = new WindowNotificationManager(mainWindow)
        {
            Position = NotificationPosition.TopRight,
            MaxItems = 3
        };
    }
}
