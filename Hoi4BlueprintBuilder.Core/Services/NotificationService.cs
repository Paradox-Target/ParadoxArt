using Avalonia.Controls.Notifications;
using Hoi4BlueprintBuilder.Core.Views;
using JetBrains.Annotations;

namespace Hoi4BlueprintBuilder.Core.Services;

[RegisterSingleton<NotificationService>]
public sealed class NotificationService(MainWindow mainWindow)
{
    private readonly WindowNotificationManager _notificationManager = new(mainWindow) { MaxItems = 3 };

    [LocalizationRequired]
    public void Show(string message)
    {
        _notificationManager.Show(message);
    }
}
