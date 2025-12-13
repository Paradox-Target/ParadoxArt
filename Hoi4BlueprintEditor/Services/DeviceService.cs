using DeviceId;

namespace Hoi4BlueprintEditor.Services;

[RegisterSingleton<DeviceService>]
public sealed class DeviceService
{
    public string GetDeviceId()
    {
        string id = new DeviceIdBuilder()
            .OnWindows(static windows => windows.AddWindowsDeviceId().AddMachineGuid())
            .ToString();
        return id;
    }
}
