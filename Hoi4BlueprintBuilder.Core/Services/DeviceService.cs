using DeviceId;

namespace Hoi4BlueprintBuilder.Core.Services;

[RegisterSingleton<DeviceService>]
public sealed class DeviceService
{
    public string GetDeviceId()
    {
        string id = new DeviceIdBuilder()
            .OnWindows(static windows => windows.AddWindowsDeviceId().AddMachineGuid())
            .OnLinux(static linux => linux.AddMachineId().AddMotherboardSerialNumber())
            .ToString();
        return id;
    }
}
