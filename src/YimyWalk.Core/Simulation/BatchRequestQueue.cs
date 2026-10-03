namespace YimyWalk.Core.Simulation;

/// <summary>Platform-neutral pending work queue used by the responsive UI batcher.</summary>
public sealed class BatchRequestQueue<T>
{
    private readonly Queue<Func<T>> _requests = new();

    public int PendingCount => _requests.Count;

    public void Enqueue(Func<T> factory, int count)
    {
        ArgumentNullException.ThrowIfNull(factory);
        if (count <= 0) return;
        for (var i = 0; i < count; i++) _requests.Enqueue(factory);
    }

    public int Drain(int maximum, Action<T> consume)
    {
        ArgumentNullException.ThrowIfNull(consume);
        var count = 0;
        for (; count < Math.Max(0, maximum) && _requests.Count > 0; count++)
            consume(_requests.Dequeue()());
        return count;
    }

    public void Cancel() => _requests.Clear();
}
