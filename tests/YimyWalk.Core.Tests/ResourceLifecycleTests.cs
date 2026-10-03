using YimyWalk.Core.Characters;
using YimyWalk.Core.Resources;

namespace YimyWalk.Core.Tests;

public sealed class ResourceLifecycleTests
{
    [Fact]
    public void DefinitionWaitsForInstanceAndRendererReferencesBeforeDisposal()
    {
        var definition = TestSpriteFactory.LoadSpriteOnly("leased");
        using var lease = new DefinitionLease(definition);
        using var instance = lease.AcquireInstance();
        using var renderer = lease.AcquireRenderer();

        lease.Dispose();
        Assert.False(definition.IsDisposed);
        instance.Dispose();
        Assert.False(definition.IsDisposed);
        renderer.Dispose();
        Assert.True(definition.IsDisposed);
    }

    [Fact]
    public void LeaseHandlesAreIdempotent()
    {
        var definition = TestSpriteFactory.LoadSpriteOnly("leased");
        var lease = new DefinitionLease(definition);
        var handle = lease.AcquireInstance();
        handle.Dispose();
        handle.Dispose();
        lease.Dispose();

        Assert.True(definition.IsDisposed);
        Assert.Equal(0, lease.ActiveInstanceCount);
        Assert.Equal(0, lease.ActiveRendererCount);
    }
}
