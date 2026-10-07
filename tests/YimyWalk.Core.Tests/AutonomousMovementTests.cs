using System.Numerics;
using YimyWalk.Core.Simulation;

namespace YimyWalk.Core.Tests;

public sealed class AutonomousMovementTests
{
    [Fact]
    public void GroundedWalkerEventuallyJumpsWithoutManualStartJump()
    {
        using var definition = TestSpriteFactory.LoadSpriteOnly("auto");
        var pet = new PetInstance(definition, new Vector2(300, 720));
        var engine = new PetEngine(new Surface(), new Random());
        engine.Tick(pet, .01);
        pet.StartWalking(FacingDirection.Right);
        var jumped = false;
        for (var i = 0; i < 2500; i++)
        {
            engine.Tick(pet, .02);
            jumped |= pet.State == PetState.Jump;
        }
        Assert.True(jumped);
    }

    [Fact]
    public void BallisticPositionDoesNotDependOnTickSubdivision()
    {
        using var definition = TestSpriteFactory.LoadSpriteOnly("ballistic");
        var first = new PetInstance(definition, new Vector2(300, 720));
        var second = new PetInstance(definition, new Vector2(300, 720));
        var engine = new PetEngine(new Surface(), new Random());
        first.StartJump();
        second.StartJump();
        engine.Tick(first, .1);
        for (var i = 0; i < 10; i++) engine.Tick(second, .01);
        Assert.Equal(first.FootPosition.Y, second.FootPosition.Y, 3);
    }

    private sealed class Surface : IWorldSurface
    {
        public WorldRect WorkingArea => new(0, 0, 1280, 720);
        public bool IsValid => true;
    }
    private sealed class Random : IRandomSource
    {
        public double NextDouble() => 0;
        public int Next(int minInclusive, int maxExclusive) => minInclusive;
    }
}
