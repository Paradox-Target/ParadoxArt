using System.Net.Http;
using System.Net.Http.Json;
using Hoi4BlueprintEditor.DTOs;

namespace Hoi4BlueprintEditor.Services;

[RegisterSingleton<AuthService>]
public sealed class AuthService : IDisposable
{
    private readonly DeviceService _deviceService;

    public AuthService(DeviceService deviceService)
    {
        _deviceService = deviceService;
        _client = new HttpClient();
#if DEBUG
        _client.BaseAddress = new Uri("https://localhost:7017/api/");
#else
        _client.BaseAddress = new Uri("https://localhost:7017/api/");
#endif
    }

    private readonly HttpClient _client;

    public async Task<bool> IsActivatedAsync()
    {
        var body = new DeviceCheckRequest { DeviceId = _deviceService.GetDeviceId() };
        using var status = await _client.PostAsJsonAsync("device/check", body);
        status.EnsureSuccessStatusCode();
        var response = await status.Content.ReadFromJsonAsync<DeviceStatusResponse>();
        return response is not null && response.IsActivated;
    }

    public async Task<DeviceStatusResponse?> ActivateDeviceAsync(string activationCode)
    {
        var body = new DeviceActivateRequest
        {
            DeviceId = _deviceService.GetDeviceId(),
            ActivationCode = activationCode
        };
        using var status = await _client.PostAsJsonAsync("device/activate", body);
        status.EnsureSuccessStatusCode();
        return await status.Content.ReadFromJsonAsync<DeviceStatusResponse>();
    }

    public void Dispose()
    {
        _client.Dispose();
    }
}
