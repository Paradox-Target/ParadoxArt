using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace Hoi4BlueprintBuilder.Core.Helpers;

public static class WindowHelper
{
    public static Window? TryGetWindow(IApplicationLifetime? lifetime)
    {
        return lifetime switch
        {
            null => null,
            IClassicDesktopStyleApplicationLifetime desktopLifetime => desktopLifetime.MainWindow,
            ISingleViewApplicationLifetime singleViewLifetime => singleViewLifetime.MainView as Window,
            _ => null
        };
    }
}
