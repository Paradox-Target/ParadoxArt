using System.Diagnostics;

namespace Hoi4BlueprintBuilder.Core.Services;

[RegisterSingleton<StatusBarService>]
public sealed class StatusBarService : IDisposable
{
    public event Action<long>? UpdateRamBytesUsage;
    public event Action<int>? UpdateFocusCount;

    private readonly Timer _ramUsageTimer;
    private readonly Process _currentProcess = Process.GetCurrentProcess();

    public StatusBarService()
    {
        _ramUsageTimer = new Timer(OnRamUsageTimerTick);
        _ramUsageTimer.Change(TimeSpan.Zero, TimeSpan.FromSeconds(3));
    }

    private void OnRamUsageTimerTick(object? state)
    {
        try
        {
            _currentProcess.Refresh();
            UpdateRamBytesUsage?.Invoke(_currentProcess.WorkingSet64);
        }
        catch
        {
            // Ignore exceptions that may occur if the process has exited or is being disposed.
        }
    }

    public void SetCurrentFocusCount(int count)
    {
        UpdateFocusCount?.Invoke(count);
    }

    public void Dispose()
    {
        _ramUsageTimer.Dispose();
        _currentProcess.Dispose();
    }
}
