using System.Numerics;
using YimyWalk.Core.Characters;
using YimyWalk.Core.Simulation;

namespace YimyWalk.Core.Tests;

public sealed class CharacterRefreshTests
{
    [Fact]
    public void FailedRefreshKeepsExistingDefinitionAndRemovedRoleStopsNewInstances()
    {
        var directory = TestSpriteFactory.WriteSprite(96, 96, fullyTransparent: false, id: "cat");
        var root = Directory.GetParent(directory)!.FullName;
        try
        {
            using var catalog = new CharacterCatalog();
            using var session = NewSession();
            var oldDefinition = catalog.Scan(root)["cat"];
            session.RegisterDefinition("cat", oldDefinition);
            var existing = session.Create("cat", new Vector2(10, 720));

            File.WriteAllBytes(Path.Combine(directory, "sprite.png"), new byte[] { 1, 2, 3 });
            catalog.Scan(root);

            Assert.True(catalog.TryGet("cat", out var retained));
            Assert.Same(oldDefinition, retained);
            Assert.Single(catalog.LastFailures);
            Assert.False(oldDefinition.IsDisposed);

            session.RemoveDefinition("cat");
            Assert.Throws<KeyNotFoundException>(() => session.Create("cat"));
            Assert.False(oldDefinition.IsDisposed);

            catalog.Dispose();
            session.Remove(existing.InstanceId);
            Assert.True(oldDefinition.IsDisposed);
        }
        finally
        {
            TestSpriteFactory.DeleteFixture(directory);
        }
    }

    private static PetSession NewSession()
        => new(new TestSurface(new WorldRect(0, 0, 1280, 720)), new FixedRandom());

    private sealed class TestSurface(WorldRect area) : IWorldSurface
    {
        public WorldRect WorkingArea { get; } = area;
        public bool IsValid => true;
    }

    private sealed class FixedRandom : IRandomSource
    {
        public double NextDouble() => 0.5;
        public int Next(int minInclusive, int maxExclusive) => minInclusive;
    }
}
