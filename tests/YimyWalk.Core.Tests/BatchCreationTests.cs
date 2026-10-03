using YimyWalk.Core.Simulation;

namespace YimyWalk.Core.Tests;

public sealed class BatchCreationTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(50)]
    [InlineData(100)]
    public void RequestsDrainInBoundedBatches(int requested)
    {
        var queue = new BatchRequestQueue<int>();
        var created = new List<int>();
        queue.Enqueue(() => requested, requested);

        Assert.Equal(requested, queue.PendingCount);
        var first = queue.Drain(8, created.Add);
        Assert.Equal(Math.Min(8, requested), first);
        Assert.True(queue.PendingCount <= Math.Max(0, requested - 8));
        while (queue.PendingCount > 0) queue.Drain(8, created.Add);

        Assert.Equal(requested, created.Count);
    }

    [Fact]
    public void CancelDropsPendingFactoriesWithoutRunningThem()
    {
        var queue = new BatchRequestQueue<int>();
        var ran = 0;
        queue.Enqueue(() => ++ran, 100);
        queue.Drain(8, _ => { });
        queue.Cancel();

        Assert.Equal(0, queue.PendingCount);
        Assert.Equal(0, queue.Drain(8, _ => { }));
        Assert.Equal(8, ran);
    }
}
