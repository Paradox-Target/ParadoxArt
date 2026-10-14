using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace Hoi4BlueprintBuilder.Core.Helpers;

public static class WindowHelper
{
    public static Window? TryGetWindow(IApplicationLifetime? lifetime)
    {
        return lifetime switch
        {
            IClassicDesktopStyleApplicationLifetime desktopLifetime => desktopLifetime.MainWindow,
            _ => null
        };
    }

    public static TopLevel? TryGetTopLevel()
    {
        return App.Current.ApplicationLifetime switch
        {
            IClassicDesktopStyleApplicationLifetime desktop => desktop.MainWindow,
            ISingleViewApplicationLifetime single => TopLevel.GetTopLevel(single.MainView),
            _ => null
        };
    }
}
