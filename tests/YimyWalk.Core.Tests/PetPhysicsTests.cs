using System.Numerics;
using YimyWalk.Core.Characters;
using YimyWalk.Core.Display;
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
    public void SpawnRemainsAboveDisplayDuringInitialFallStep()
    {
        var pet = NewPet(new Vector2(100, -32));
        var engine = NewEngine();

        engine.Tick(pet, 0.016);

        Assert.True(pet.FootPosition.Y < Area.Top);
        Assert.Equal(PetState.Fall, pet.State);
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
    public void AirborneLandingUsesProjectedHorizontalPosition()
    {
        var pet = NewPet(new Vector2(50, 250));
        var surface = new ProjectedSurface(Area);
        var engine = new PetEngine(surface, new FixedRandom());
        engine.BeginDrag(pet);
        engine.ReleaseDrag(pet, new Vector2(1000, 500));

        engine.Tick(pet, 0.1);

        Assert.Equal(150, surface.LastNextPosition.X, 3);
        Assert.Equal(315, pet.FootPosition.Y, 3);
        Assert.Equal(150, pet.FootPosition.X, 3);
        Assert.Equal(PetState.Fall, pet.State);
    }

    [Fact]
    public void WalkingOffWindowSurfaceFallsInsteadOfReversingAtEdge()
    {
        var pet = NewPet(new Vector2(95, 300));
        pet.StartWalking(FacingDirection.Right);
        var engine = new PetEngine(new ProjectedSurface(Area), new FixedRandom());

        engine.Tick(pet, 0.1);

        Assert.Equal(PetState.Fall, pet.State);
        Assert.Equal(101, pet.FootPosition.X, 3);
        Assert.Equal(315, pet.FootPosition.Y, 3);
    }

    [Fact]
    public void SpriteOverlapWithoutFootContactDoesNotSnapOntoLedge()
    {
        var pet = NewPet(new Vector2(120, 295));
        var engine = new PetEngine(new ProjectedSurface(Area), new FixedRandom());
        engine.Tick(pet, 0.1);
        Assert.Equal(120, pet.FootPosition.X, 3);
        Assert.Equal(PetState.Fall, pet.State);
        Assert.True(pet.FootPosition.Y > 300);
    }

    [Fact]
    public void MissingDisplayRelocatesIdlePetToAvailableFloor()
    {
        var surface = new TestSurface(Area);
        var engine = new PetEngine(surface, new FixedRandom());
        var pet = NewPet(new Vector2(100, Area.Bottom));
        engine.Tick(pet, 0.1);
        surface.WorkingArea = new WorldRect(-800, 0, 0, 600);
        surface.IsValid = false;
        engine.Tick(pet, 0.1);
        Assert.InRange(pet.FootPosition.X, -800, 0);
        Assert.InRange(pet.FootPosition.Y, 0, 600);
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
        public WorldRect WorkingArea { get; set; } = area;
        public bool IsValid { get; set; } = true;
    }

    private sealed class ProjectedSurface(WorldRect area) : IWorldSurface
    {
        private readonly WindowSurfaceCandidate[] _candidates =
        {
            new("ledge", new WorldRect(0, 300, 100, 500), true, false, false, false)
        };

        public WorldRect WorkingArea { get; } = area;
        public bool IsValid => true;
        public Vector2 LastNextPosition { get; private set; }

        public SurfaceSelection SurfaceForPet(Vector2 footPosition, float footHalfWidth, Vector2 nextFootPosition)
        {
            LastNextPosition = nextFootPosition;
            return SurfaceSelector.Select(
                _candidates,
                WorkingArea,
                nextFootPosition.X - footHalfWidth,
                nextFootPosition.X + footHalfWidth,
                footPosition.Y,
                nextFootPosition.Y);
        }
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
