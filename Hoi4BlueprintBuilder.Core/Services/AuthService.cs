using System.Net.Http.Json;
using System.Net.Security;
using System.Security.Cryptography;
using Hoi4BlueprintBuilder.Core.DTOs;
using NLog;

namespace Hoi4BlueprintBuilder.Core.Services;

[RegisterSingleton<AuthService>]
public sealed class AuthService : IDisposable
{
    private readonly DeviceService _deviceService;
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public AuthService(DeviceService deviceService)
    {
        _deviceService = deviceService;
        var handler = new HttpClientHandler();
        handler.ServerCertificateCustomValidationCallback = static (_, cert, _, errors) =>
        {
            if (errors == SslPolicyErrors.None)
            {
                return true;
            }

            string? actualThumbprint = cert?.GetCertHashString(HashAlgorithmName.SHA256);

            return string.Equals(
                actualThumbprint,
                Private.ExpectedThumbprint,
                StringComparison.OrdinalIgnoreCase
            );
        };
        _client = new HttpClient(handler);
        _client.Timeout = TimeSpan.FromSeconds(15);
#if DEBUG
        _client.BaseAddress = new Uri("https://localhost:7017/api/");
#else
        _client.BaseAddress = new Uri("https://47.96.168.220:7017/api/");
#endif
    }

    private readonly HttpClient _client;

    public async Task<bool> IsActivatedAsync()
    {
        string id = _deviceService.GetDeviceId();
        Log.Info("User ID: {Id}", id);
        var body = new DeviceCheckRequest { DeviceId = id };
        using var result = await _client.PostAsJsonAsync("device/check", body);
        result.EnsureSuccessStatusCode();
        var response = await result.Content.ReadFromJsonAsync<DeviceStatusResponse>();
        Log.Debug("IsActivated 服务器查询结果: {@}", response);

        return response is not null && response.IsActivated;
    }

    public async Task<DeviceStatusResponse?> ActivateDeviceAsync(string activationCode)
    {
        var body = new DeviceActivateRequest
        {
            DeviceId = _deviceService.GetDeviceId(),
            ActivationCode = activationCode
        };
        using var result = await _client.PostAsJsonAsync("device/activate", body);
        result.EnsureSuccessStatusCode();
        return await result.Content.ReadFromJsonAsync<DeviceStatusResponse>();
    }

    public void Dispose()
    {
        _client.Dispose();
    }
}
