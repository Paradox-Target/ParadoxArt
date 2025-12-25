using Hoi4BlueprintBuilder.Core.Models;

namespace Hoi4BlueprintBuilder.Core.Services;

[RegisterSingleton<UserStatusService>]
public sealed class UserStatusService
{
    public SystemFileItem? CurrentSelectedFile { get; set; }
}
