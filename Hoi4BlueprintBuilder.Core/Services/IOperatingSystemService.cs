namespace Hoi4BlueprintBuilder.Core.Services;

public interface IOperatingSystemService
{
    void ShutdownBlockReasonCreate(string reason);
    void ShutdownBlockReasonDestroy();
}
