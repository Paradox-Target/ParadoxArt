using DeviceId;
using Hoi4BlueprintBuilder.Core.Infrastructure;

namespace Hoi4BlueprintBuilder.Core.Services;

[RegisterSingleton<DeviceService>]
public sealed class DeviceService
{
    public string GetDeviceId()
    {
        string id = new DeviceIdBuilder()
            .OnAndroid(static android => android.AddAndroidDeviceId())
            .OnWindows(static windows => windows.AddWindowsDeviceId().AddMachineGuid())
            .OnLinux(static linux => linux.AddMachineId().AddMotherboardSerialNumber())
            .ToString();
        return id;
    }
}
