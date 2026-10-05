using System.Text.Json;

namespace YimyWalk.App.Settings;

internal enum SettingsSaveState
{
    Applied,
    Saving,
    Saved,
    Failed
}

internal sealed class SettingsSaveCoordinator : IDisposable
{
    private readonly System.Windows.Forms.Timer _timer;
    private readonly Action _save;
    private bool _disposed;

    public SettingsSaveCoordinator(Action save)
    {
        _save = save ?? throw new ArgumentNullException(nameof(save));
        _timer = new System.Windows.Forms.Timer { Interval = 300 };
        _timer.Tick += (_, _) => Flush();
        State = SettingsSaveState.Saved;
    }

    public SettingsSaveState State { get; private set; }
    public string? Error { get; private set; }
    public event Action? Changed;

    public void Request(bool debounce)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        Error = null;
        State = SettingsSaveState.Applied;
        Changed?.Invoke();
        if (debounce)
        {
            _timer.Stop();
            _timer.Start();
        }
        else
        {
            Flush();
        }
    }

    public void Flush()
    {
        if (_disposed) return;
        _timer.Stop();
        State = SettingsSaveState.Saving;
        Error = null;
        Changed?.Invoke();
        try
        {
            _save();
            State = SettingsSaveState.Saved;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            State = SettingsSaveState.Failed;
            Error = ex.Message;
        }
        Changed?.Invoke();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _timer.Stop();
        _timer.Dispose();
        Changed = null;
    }
}
