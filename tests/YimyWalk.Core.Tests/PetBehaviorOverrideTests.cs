using System.Numerics;
using YimyWalk.Core.Simulation;

namespace YimyWalk.Core.Tests;

public sealed class PetBehaviorOverrideTests
{
    [Fact]
    public void BooleanOverridesResolveAgainstGlobalDefault()
    {
        Assert.True(BooleanOverride.Inherit.Resolve(true));
        Assert.True(BooleanOverride.Enabled.Resolve(false));
        Assert.False(BooleanOverride.Disabled.Resolve(true));
    }

    [Fact]
    public void ActivityOverrideUsesGlobalWhenInheritedAndClampsCustomValue()
    {
        Assert.Equal(1.5f, PetBehaviorOverrides.Inherit.ResolveActivityRate(1.5f));
        Assert.Equal(4f, new PetBehaviorOverrides(ActivityRateOverride: 9f).ResolveActivityRate(1.5f));
    }

    [Fact]
    public void ChangingGlobalWalkDefaultOnlyChangesInheritedInstances()
    {
        var inherited = PetBehaviorOverrides.Inherit;
        var forcedOn = new PetBehaviorOverrides(WalkOverride: BooleanOverride.Enabled);
        var forcedOff = new PetBehaviorOverrides(WalkOverride: BooleanOverride.Disabled);

        Assert.True(inherited.ResolveWalkEnabled(true));
        Assert.False(inherited.ResolveWalkEnabled(false));
        Assert.True(forcedOn.ResolveWalkEnabled(false));
        Assert.False(forcedOff.ResolveWalkEnabled(true));
    }

    [Fact]
    public void ResettingActivityOverrideReturnsToTheGlobalValue()
    {
        var custom = new PetBehaviorOverrides(ActivityRateOverride: 0.6f);

        Assert.Equal(0.6f, custom.ResolveActivityRate(1.8f));
        Assert.Equal(1.8f, (custom with { ActivityRateOverride = null }).ResolveActivityRate(1.8f));
    }

    [Fact]
    public void InstanceScaleOverrideStaysIndependentFromGlobalScaleUntilReset()
    {
        var inherited = new PetInstance(TestSpriteFactory.LoadSpriteOnly("scale-inherited"), new Vector2(100, 720));
        var custom = new PetInstance(TestSpriteFactory.LoadSpriteOnly("scale-custom"), new Vector2(300, 720))
        {
            ScaleOverride = 0.6f
        };

        Assert.Equal(1.5f, inherited.ResolveScale(1.5f));
        Assert.Equal(0.6f, custom.ResolveScale(1.5f));
        Assert.Equal(0.6f, custom.ResolveScale(2.5f));

        custom.ScaleOverride = null;

        Assert.Equal(2.5f, custom.ResolveScale(2.5f));
    }

    [Fact]
    public void DisabledWalkingDoesNotDisableGravityOrDragging()
    {
        var pet = new PetInstance(TestSpriteFactory.LoadSpriteOnly("override"), new Vector2(100, 720))
        {
            BehaviorOverrides = new PetBehaviorOverrides(WalkOverride: BooleanOverride.Disabled)
        };
        var engine = new PetEngine(new TestSurface(), new FixedRandom())
        {
            DefaultWalkEnabled = true
        };

        for (var i = 0; i < 100 && pet.State == PetState.Fall; i++)
            engine.Tick(pet, 0.1);
        for (var i = 0; i < 100; i++)
            engine.Tick(pet, 0.1);

        Assert.NotEqual(PetState.Walk, pet.State);
        engine.BeginDrag(pet);
        engine.SetDraggedFootPosition(pet, new Vector2(300, 300));
        engine.ReleaseDrag(pet, new Vector2(0, 300));
        engine.Tick(pet, 0.1);
        Assert.Equal(PetState.Fall, pet.State);
        Assert.True(pet.FootPosition.Y > 300);
    }

    private sealed class TestSurface : IWorldSurface
    {
        public WorldRect WorkingArea => new(0, 0, 1280, 720);
        public bool IsValid => true;
    }

    private sealed class FixedRandom : IRandomSource
    {
        public double NextDouble() => 0.5;
        public int Next(int minInclusive, int maxExclusive) => minInclusive;
    }
}
