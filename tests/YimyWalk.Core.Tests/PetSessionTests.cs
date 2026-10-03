using System.Numerics;
using YimyWalk.Core.Characters;
using YimyWalk.Core.Simulation;

namespace YimyWalk.Core.Tests;

public sealed class PetSessionTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(50)]
    [InlineData(100)]
    public void CreateManyReturnsRequestedCountAndClearRemovesAll(int count)
    {
        using var session = NewSession();
        var definition = TestSpriteFactory.LoadSpriteOnly("cat");

        var created = session.CreateMany(definition, count);

        Assert.Equal(count, created.Count);
        Assert.Equal(count, session.ActiveCount);
        Assert.All(created, pet => Assert.Same(definition, pet.Definition));
        session.Clear();
        Assert.Empty(session.Instances);
        Assert.Equal(0, session.ActiveCount);
    }

    [Fact]
    public void InstancesHaveIndependentStateAndEventsAreDetachable()
    {
        using var session = NewSession();
        var definition = TestSpriteFactory.LoadSpriteOnly("cat");
        var added = 0;
        var removed = 0;
        session.InstanceAdded += _ => added++;
        session.InstanceRemoved += _ => removed++;

        var pets = session.CreateMany(definition, 2);
        pets[0].StartWalking(FacingDirection.Right);

        Assert.Equal(PetState.Walk, pets[0].State);
        Assert.Equal(PetState.Fall, pets[1].State);
        Assert.Equal(2, added);
        session.Remove(pets[0].InstanceId);
        Assert.Equal(1, removed);
        session.Clear();
        Assert.Equal(2, removed);
    }

    [Fact]
    public void ReplacementUsesNewDefinitionForNewInstancesAndRetainsOldForExisting()
    {
        using var session = NewSession();
        var oldDefinition = TestSpriteFactory.LoadSpriteOnly("cat");
        var newDefinition = TestSpriteFactory.LoadSpriteOnly("cat");
        session.RegisterDefinition("cat", oldDefinition);
        var oldPet = session.Create("cat", new Vector2(10, 720));

        session.ReplaceDefinition("cat", newDefinition);
        var newPet = session.Create("cat", new Vector2(20, 720));

        Assert.Same(oldDefinition, oldPet.Definition);
        Assert.Same(newDefinition, newPet.Definition);
        Assert.False(oldDefinition.IsDisposed);
        session.Remove(oldPet.InstanceId);
        Assert.True(oldDefinition.IsDisposed);
    }

    [Fact]
    public void ConfiguredHardLimitIsNotAProductConstant()
    {
        using var session = NewSession(hardLimit: 3);
        var definition = TestSpriteFactory.LoadSpriteOnly("cat");

        Assert.Equal(3, session.CreateMany(definition, 10).Count);
        Assert.Equal(3, session.ActiveCount);
    }

    private static PetSession NewSession(int? hardLimit = null)
        => new(new TestSurface(new WorldRect(0, 0, 1280, 720)), new FixedRandom(), hardLimit);

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
