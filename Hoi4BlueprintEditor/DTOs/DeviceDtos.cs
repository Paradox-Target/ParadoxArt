using System.ComponentModel.DataAnnotations;

namespace Hoi4BlueprintEditor.DTOs;

public sealed class DeviceCheckRequest
{
    [Required]
    public required string DeviceId { get; set; }
}

public sealed class DeviceActivateRequest
{
    [Required]
    public required string DeviceId { get; set; }

    [Required]
    public required string ActivationCode { get; set; }
}

public sealed class DeviceStatusResponse
{
    public bool IsActivated { get; set; }
    public string Message { get; set; } = string.Empty;
}