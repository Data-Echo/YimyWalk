using System.Numerics;
using YimyWalk.Core.Characters;
using YimyWalk.Core.Simulation;

namespace YimyWalk.Core.Tests;

public sealed class PetPhysicsTests
{
    private static readonly WorldRect Area = new(0, 0, 1280, 720);

    [Fact]
    public void SpawnAboveAreaFallsToGroundThenBecomesIdle()
    {
        var pet = NewPet(new Vector2(100, -40));
        var engine = NewEngine();

        engine.Tick(pet, 1.0); // clamped to one tenth, still airborne
        Assert.Equal(PetState.Fall, pet.State);
        for (var i = 0; i < 100 && pet.State != PetState.Idle; i++) engine.Tick(pet, 0.1);

        Assert.Equal(Area.Bottom, pet.FootPosition.Y, 3);
        Assert.Equal(PetState.Idle, pet.State);
        Assert.Equal(FrameKind.Idle, pet.GetFrameKind());
    }

    [Fact]
    public void WalkingReversesAtWorkingAreaEdges()
    {
        var pet = NewPet(new Vector2(2, Area.Bottom));
        pet.StartWalking(FacingDirection.Left);
        var engine = NewEngine();

        engine.Tick(pet, 0.1);

        Assert.Equal(Area.Left, pet.FootPosition.X, 3);
        Assert.Equal(FacingDirection.Right, pet.Facing);
        Assert.Equal(FrameKind.WalkRight, pet.GetFrameKind());
    }

    [Fact]
    public void JumpSelectsApexOnlyNearZeroVelocityAndThenFalls()
    {
        var pet = NewPet(new Vector2(100, Area.Bottom));
        var engine = NewEngine();
        pet.StartJump();

        Assert.Equal(FrameKind.Jump, pet.GetFrameKind());
        for (var i = 0; i < 20 && pet.State != PetState.Apex; i++) engine.Tick(pet, 0.05);

        Assert.Equal(PetState.Apex, pet.State);
        Assert.Equal(FrameKind.Apex, pet.GetFrameKind());
        engine.Tick(pet, 0.1);
        Assert.Equal(PetState.Fall, pet.State);
        Assert.Equal(FrameKind.Fall, pet.GetFrameKind());
    }

    [Fact]
    public void DraggingDisablesAutonomousPhysicsAndReleaseAppliesCappedToss()
    {
        var pet = NewPet(new Vector2(300, Area.Bottom));
        var engine = NewEngine();
        engine.BeginDrag(pet);
        engine.SetDraggedFootPosition(pet, new Vector2(500, 300));
        engine.Tick(pet, 0.1);

        Assert.Equal(PetState.Dragged, pet.State);
        Assert.Equal(new Vector2(500, 300), pet.FootPosition);
        Assert.Equal(Vector2.Zero, pet.Velocity);

        engine.ReleaseDrag(pet, new Vector2(5000, -5000));
        Assert.Equal(PetState.Jump, pet.State);
        Assert.True(Math.Abs(pet.Velocity.X) <= PetEngine.MaxTossSpeed);
        Assert.True(Math.Abs(pet.Velocity.Y) <= PetEngine.MaxTossSpeed);
    }

    [Fact]
    public void UpwardTossUsesJumpAndApexFramesBeforeFalling()
    {
        var pet = NewPet(new Vector2(300, Area.Bottom));
        var engine = NewEngine();
        engine.BeginDrag(pet);
        engine.SetDraggedFootPosition(pet, new Vector2(300, 500));
        engine.ReleaseDrag(pet, new Vector2(0, -400));

        Assert.Equal(PetState.Jump, pet.State);
        Assert.Equal(FrameKind.Jump, pet.GetFrameKind());
        for (var i = 0; i < 20 && pet.State != PetState.Apex; i++) engine.Tick(pet, 0.05);
        Assert.Equal(PetState.Apex, pet.State);
        Assert.Equal(FrameKind.Apex, pet.GetFrameKind());
    }

    [Fact]
    public void InstancesKeepIndependentMutableStateAndMissingSurfaceRelocatesSafely()
    {
        var first = NewPet(new Vector2(100, Area.Bottom));
        var second = NewPet(new Vector2(300, Area.Bottom));
        var engine = NewEngine();
        first.StartWalking(FacingDirection.Right);
        engine.Tick(first, 0.1);

        Assert.NotEqual(first.FootPosition, second.FootPosition);
        Assert.Equal(Vector2.Zero, second.Velocity);

        var surface = (TestSurface)engine.Surface;
        surface.IsValid = false;
        engine.Tick(first, 0.1);
        Assert.InRange(first.FootPosition.X, Area.Left, Area.Right);
        Assert.Equal(Area.Bottom, first.FootPosition.Y);
    }

    [Fact]
    public void IdleEventuallyLeavesByConfiguredMaximum()
    {
        var pet = NewPet(new Vector2(100, Area.Bottom));
        var engine = new PetEngine(new TestSurface(Area), new LowRandom());
        for (var i = 0; i < 90 && pet.State == PetState.Fall; i++) engine.Tick(pet, 0.1);
        for (var i = 0; i < 90 && pet.State == PetState.Idle; i++) engine.Tick(pet, 0.1);
        Assert.Equal(PetState.Walk, pet.State);
    }

    private static PetInstance NewPet(Vector2 foot)
        => new(TestSpriteFactory.LoadSpriteOnly("pet"), foot);

    private static PetEngine NewEngine()
        => new(new TestSurface(Area), new FixedRandom());

    private sealed class TestSurface(WorldRect area) : IWorldSurface
    {
        public WorldRect WorkingArea { get; } = area;
        public bool IsValid { get; set; } = true;
    }

    private sealed class FixedRandom : IRandomSource
    {
        public double NextDouble() => 0.5;
        public int Next(int minInclusive, int maxExclusive) => minInclusive;
    }

    private sealed class LowRandom : IRandomSource
    {
        public double NextDouble() => 0;
        public int Next(int minInclusive, int maxExclusive) => minInclusive;
    }
}
