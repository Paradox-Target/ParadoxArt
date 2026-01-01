using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hoi4BlueprintBuilder.Core.DTOs;

public sealed class DeviceCheckRequest
{
    [Required]
    public required string DeviceId { get; set; }
}

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(DeviceCheckRequest))]
internal partial class DeviceCheckRequestContext : JsonSerializerContext;

public sealed class DeviceActivateRequest
{
    [Required]
    public required string DeviceId { get; set; }

    [Required]
    public required string ActivationCode { get; set; }
}

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(DeviceActivateRequest))]
internal partial class DeviceActivateRequestContext : JsonSerializerContext;

public sealed class DeviceStatusResponse
{
    public bool IsActivated { get; set; }
    public string Message { get; set; } = string.Empty;
}

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(DeviceStatusResponse))]
internal partial class DeviceStatusResponseContext : JsonSerializerContext;
