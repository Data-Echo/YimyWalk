using YimyWalk.Core.Simulation;

namespace YimyWalk.App.Host;

internal sealed class BatchCreationQueue : IDisposable
{
    private readonly System.Windows.Forms.Timer _timer;
    private readonly BatchRequestQueue<PetInstance> _work = new();
    private bool _disposed;

    public BatchCreationQueue(Func<PetInstance> createOne)
    {
        CreateOne = createOne ?? throw new ArgumentNullException(nameof(createOne));
        _timer = new System.Windows.Forms.Timer { Interval = 1 };
        _timer.Tick += DrainBatch;
    }

    public int PendingCount => _work.PendingCount;
    public int ActiveCount { get; private set; }
    public Func<PetInstance> CreateOne { get; set; }
    public event Action? Changed;

    public void Enqueue(int count)
        => Enqueue(CreateOne, count);

    public void Enqueue(Func<PetInstance> factory, int count)
    {
        if (_disposed || count <= 0) return;
        ArgumentNullException.ThrowIfNull(factory);
        _work.Enqueue(factory, count);
        _timer.Start();
        Changed?.Invoke();
    }

    public void Cancel()
    {
        _work.Cancel();
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
        if (_work.PendingCount <= 0)
        {
            _timer.Stop();
            return;
        }

        _work.Drain(8, _ =>
        {
            ActiveCount++;
        });
        if (_work.PendingCount <= 0) _timer.Stop();
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
