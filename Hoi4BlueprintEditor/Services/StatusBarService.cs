using System.Diagnostics;

namespace Hoi4BlueprintEditor.Services;

[RegisterSingleton<StatusBarService>]
public sealed class StatusBarService : IDisposable
{
    public event Action<string>? UpdateRamUsage;
    public event Action<string>? UpdateFocusCount;

    private readonly Timer _ramUsageTimer;
    private readonly Process _currentProcess = Process.GetCurrentProcess();

    public StatusBarService()
    {
        _ramUsageTimer = new Timer(OnRamUsageTimerTick);
        _ramUsageTimer.Change(TimeSpan.Zero, TimeSpan.FromSeconds(2));
    }

    private void OnRamUsageTimerTick(object? state)
    {
        try
        {
            _currentProcess.Refresh();
            long memoryUsageInMB = _currentProcess.WorkingSet64 / (1024 * 1024);
            UpdateRamUsage?.Invoke($"内存使用: {memoryUsageInMB} MB");
        }
        catch
        {
            // Ignore exceptions that may occur if the process has exited or is being disposed.
        }
    }

    public void SetCurrentFocusCount(int count)
    {
        UpdateFocusCount?.Invoke($"国策数量: {count}");
    }

    public void Dispose()
    {
        _ramUsageTimer.Dispose();
        _currentProcess.Dispose();
    }
}
