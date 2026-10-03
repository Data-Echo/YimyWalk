using YimyWalk.Core.Characters;
using YimyWalk.Core.Resources;

namespace YimyWalk.Core.Tests;

public sealed class ResourceLifecycleTests
{
    [Fact]
    public void SeparateCatalogSessionAndRendererOwnersShareTheSameLifetime()
    {
        var definition = TestSpriteFactory.LoadSpriteOnly("owners");
        var catalog = new DefinitionLease(definition);
        var session = new DefinitionLease(definition);
        var renderer = new DefinitionLease(definition);
        var instance = session.AcquireInstance();
        var paint = renderer.AcquireRenderer();
        catalog.Dispose();
        session.Dispose();
        Assert.False(definition.IsDisposed);
        instance.Dispose();
        renderer.Dispose();
        Assert.False(definition.IsDisposed);
        paint.Dispose();
        Assert.True(definition.IsDisposed);
        Assert.Empty(definition.Frames);
    }

    [Fact]
    public void ReplacingUnreferencedCatalogVersionsReleasesThem()
    {
        var directory = TestSpriteFactory.WriteSprite(96, 96, false);
        using var catalog = new CharacterCatalog();
        var root = Path.GetDirectoryName(directory)!;
        var first = catalog.Scan(root).Values.Single();
        var next = catalog.Scan(root).Values.Single();
        Assert.True(first.IsDisposed);
        Assert.False(next.IsDisposed);
    }
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
