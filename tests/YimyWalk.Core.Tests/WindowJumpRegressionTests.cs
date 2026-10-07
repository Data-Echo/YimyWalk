using System.Numerics;
using YimyWalk.Core.Characters;
using YimyWalk.Core.Display;
using YimyWalk.Core.Simulation;

namespace YimyWalk.Core.Tests;

public sealed class WindowJumpRegressionTests
{
    [Theory]
    [InlineData(1080, 300, 1, .016)]
    [InlineData(1740, 400, 1.25, .033)]
    [InlineData(1080, 300, 1, .1)]
    public void FullWindowDesireChargesAndLandsAcrossRealDesktopHeight(float floor, float top, float scale, double dt)
    {
        var directory = TestSpriteFactory.WriteSprite(1254, 1254, false);
        try
        {
            using var definition = CharacterLoader.LoadDirectory(directory);
            var world = new World(floor, new WindowSurfaceCandidate("high", new(200, top, 1500, top + 200), true, false, false, false));
            var left = 200 + definition.Anchor.X * scale + 4;
            var pet = Walker(definition, new(left - 40, floor));
            var engine = new PetEngine(world, new ZeroRandom()) { JumpDesire = 1, WindowJumpDesire = 1, GlobalScale = scale, ClimbDesire = 0 };
            engine.Tick(pet, .01);
            Assert.Equal(PetState.JumpCharge, pet.State);
            Assert.Equal(FrameKind.Apex, pet.GetFrameKind());
            var start = pet.FootPosition;
            engine.Tick(pet, .1);
            Assert.Equal(start, pet.FootPosition);
            var sawJump = false;
            for (var i = 0; i < 800 && pet.State != PetState.Idle; i++)
            {
                engine.Tick(pet, dt);
                sawJump |= pet.State == PetState.Jump;
                Assert.InRange(Math.Abs(pet.Velocity.X), 0, 60);
            }
            Assert.True(sawJump);
            Assert.Equal(PetState.Idle, pet.State);
            Assert.Equal(top, pet.FootPosition.Y, 2);
            Assert.InRange(pet.FootPosition.X, left, 1500 - definition.CellWidth * scale / 2 - 4);
        }
        finally { TestSpriteFactory.DeleteFixture(directory); }
    }

    [Fact]
    public void BallisticAdvanceMatchesPlannerAndTickSubdivision()
    {
        var whole = BallisticMotion.Advance(-1200, 1500, 1500, .1f);
        var velocity = -1200f;
        var displacement = 0f;
        for (var i = 0; i < 10; i++)
        {
            var step = BallisticMotion.Advance(velocity, 1500, 1500, .01f);
            velocity = step.Velocity;
            displacement += step.Displacement;
        }
        Assert.Equal(-112.5f, whole.Displacement, 3);
        Assert.Equal(whole.Displacement, displacement, 3);
        Assert.Equal(whole.Velocity, velocity, 3);
        // A step that reaches terminal speed partway through must split there.
        Assert.Equal(148.75f, BallisticMotion.Advance(1450, 1000, 1500, .1f).Displacement, 3);
    }

    [Fact]
    public void ZeroGravityPreservesConstantVelocityMotion()
    {
        var result = BallisticMotion.Advance(-200, 0, 1500, .1f);
        Assert.Equal(-20, result.Displacement);
        Assert.Equal(-200, result.Velocity);
    }

    [Fact]
    public void WindowLaunchCeilingStillRejectsAnUnreachableHeight()
    {
        WindowSurfaceCandidate[] windows = [new("too-high", new(0, 100, 1000, 300), true, false, false, false)];
        Assert.False(JumpTargetPlanner.TrySelect(windows, new(500, 3000), FacingDirection.Right,
            60, 450, 1500, 1500, 20, out _, new(0, 0, 1920, 3000), 20,
            450 * PetEngine.WindowJumpSpeedMultiplier));
    }

    [Fact]
    public void PlannerUsesExtraChargeForHorizontalTravelWithoutSpeedingUpWalking()
    {
        WindowSurfaceCandidate[] windows = [new("farther", new(30, 600, 300, 800), true, false, false, false)];
        Assert.True(JumpTargetPlanner.TrySelect(windows, new(0, 1080), FacingDirection.Right,
            60, 450, 1500, 1500, 20, out var target, new(0, 0, 1920, 1080), 20, 2700));
        Assert.InRange(target.HorizontalVelocity, 0, 60);
        Assert.True(target.LaunchSpeed > 450);
        var result = BallisticMotion.Advance(-target.LaunchSpeed, 1500, 1500, target.LandingTime);
        Assert.Equal(600f, 1080 + result.Displacement, 2);
        Assert.True(result.Velocity > 0);
    }

    [Fact]
    public void WindowAttemptSkipsCurrentSupportingWindow()
    {
        using var definition = TestSpriteFactory.LoadSpriteOnly("support-skip");
        WindowSurfaceCandidate[] windows = [
            new("support", new(100, 600, 1000, 800), true, false, false, false),
            new("destination", new(510, 400, 1000, 580), true, false, false, false)];
        var pet = Walker(definition, new(500, 600));
        var engine = new PetEngine(new World(1080, windows), new ZeroRandom()) { WindowJumpDesire = 1, JumpDesire = 0 };
        engine.Tick(pet, .01);
        Assert.Equal(PetState.JumpCharge, pet.State);
        for (var i = 0; i < 500 && pet.State != PetState.Idle; i++) engine.Tick(pet, .016);
        Assert.Equal(400, pet.FootPosition.Y, 2);
    }

    private static PetInstance Walker(CharacterDefinition definition, Vector2 position)
    {
        var pet = new PetInstance(definition, position) { AutonomousTimersInitialized = true, ClimbAttemptDelay = 999 };
        pet.StartWalking(FacingDirection.Right);
        return pet;
    }

    private sealed class World(float floor, params WindowSurfaceCandidate[] windows) : IWorldSurface
    {
        public WorldRect WorkingArea => new(0, 0, 1920, floor);
        public bool IsValid => true;
        public bool WindowLandingEnabled => true;
        public IReadOnlyList<WindowSurfaceCandidate> JumpCandidates { get; set; } = windows;
        public SurfaceSelection SurfaceForPet(Vector2 start, float halfWidth, Vector2 end)
            => SurfaceSelector.Select(JumpCandidates, WorkingArea, end.X - halfWidth, end.X + halfWidth, start.Y, end.Y);
    }

    private sealed class ZeroRandom : IRandomSource
    {
        public double NextDouble() => 0;
        public int Next(int minInclusive, int maxExclusive) => minInclusive;
    }
}
