namespace Hoi4BlueprintBuilder.Core.Services;

public sealed class DefaultOperatingSystemService : IOperatingSystemService
{
    public void ShutdownBlockReasonCreate(string reason)
    {
        // 不支持的操作系统，什么都不做
    }

    public void ShutdownBlockReasonDestroy()
    {
        // 不支持的操作系统，什么都不做
    }
}
