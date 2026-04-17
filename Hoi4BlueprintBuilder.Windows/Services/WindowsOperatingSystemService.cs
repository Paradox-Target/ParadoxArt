using Hoi4BlueprintBuilder.Core.Services;
using Hoi4BlueprintBuilder.Core.Views;
using Microsoft.Extensions.DependencyInjection;
using Vanara.PInvoke;

namespace Hoi4BlueprintBuilder.Windows.Services;

public sealed class WindowsOperatingSystemService(IServiceProvider serviceProvider) : IOperatingSystemService
{
    public void ShutdownBlockReasonCreate(string reason)
    {
        var handle = serviceProvider.GetRequiredService<MainWindow>().TryGetPlatformHandle();
        if (handle is null)
        {
            return;
        }

        User32.ShutdownBlockReasonCreate(handle.Handle, reason);
    }

    public void ShutdownBlockReasonDestroy()
    {
        var handle = serviceProvider.GetRequiredService<MainWindow>().TryGetPlatformHandle();
        if (handle is null)
        {
            return;
        }

        User32.ShutdownBlockReasonDestroy(handle.Handle);
    }
}
