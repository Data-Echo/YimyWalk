using YimyWalk.Core.Simulation;

namespace YimyWalk.App.Host;

internal sealed class BatchCreationQueue : IDisposable
{
    private readonly System.Windows.Forms.Timer _timer;
    private int _pending;
    private bool _disposed;

    public BatchCreationQueue(Func<PetInstance> createOne)
    {
        CreateOne = createOne ?? throw new ArgumentNullException(nameof(createOne));
        _timer = new System.Windows.Forms.Timer { Interval = 1 };
        _timer.Tick += DrainBatch;
    }

    public int PendingCount => _pending;
    public int ActiveCount { get; private set; }
    public Func<PetInstance> CreateOne { get; set; }
    public event Action? Changed;

    public void Enqueue(int count)
    {
        if (_disposed || count <= 0) return;
        _pending = checked(_pending + count);
        _timer.Start();
        Changed?.Invoke();
    }

    public void Cancel()
    {
        _pending = 0;
        _timer.Stop();
        Changed?.Invoke();
    }

    public void NotifyRemoved()
    {
        if (ActiveCount > 0) ActiveCount--;
        Changed?.Invoke();
    }

    private void DrainBatch(object? sender, EventArgs e)
    {
        if (_pending <= 0)
        {
            _timer.Stop();
            return;
        }

        var batch = Math.Min(8, _pending);
        for (var i = 0; i < batch; i++)
        {
            CreateOne();
            ActiveCount++;
            _pending--;
        }
        if (_pending <= 0) _timer.Stop();
        Changed?.Invoke();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Cancel();
        _timer.Dispose();
        Changed = null;
    }
}
