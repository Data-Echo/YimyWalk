using System.Numerics;
using YimyWalk.Core.Display;
using YimyWalk.Core.Simulation;

namespace YimyWalk.Core.Tests;

public sealed class AutonomousBoundaryTests
{
    [Fact]
    public void WindowJumpUsesReachableLandingSurface()
    {
        using var definition = TestSpriteFactory.LoadSpriteOnly("window-jump");
        var surface = new World { WindowLandingEnabled = true };
        surface.JumpCandidates = [new("target", new(300, 710, 500, 800), true, false, false, false)];
        var pet = NewWalker(definition, new(300, 720));
        var engine = new PetEngine(surface, new FixedRandom()) { JumpDesire = 1, ClimbDesire = 0 };
        engine.Tick(pet, .01);
        Assert.Equal(PetState.JumpCharge, pet.State);
        for (var i = 0; i < 200 && pet.State != PetState.Idle; i++) engine.Tick(pet, .01);
        Assert.Equal(PetState.Idle, pet.State);
        Assert.Equal(710, pet.FootPosition.Y, 3);
        Assert.InRange(pet.FootPosition.X, 320, 480);
    }

    [Fact]
    public void HighWindowJumpUsesAChargedTrajectoryAndReachesTheTop()
    {
        using var definition = TestSpriteFactory.LoadSpriteOnly("high-window-jump");
        var surface = new World { WindowLandingEnabled = true };
        surface.JumpCandidates = [new("high-target", new(300, 600, 500, 680), true, false, false, false)];
        var pet = NewWalker(definition, new(300, 720));
        var engine = new PetEngine(surface, new FixedRandom())
        {
            JumpDesire = 0,
            WindowJumpDesire = 1,
            ClimbDesire = 0
        };

        engine.Tick(pet, .01);

        Assert.Equal(PetState.JumpCharge, pet.State);
        for (var i = 0; i < 200 && (pet.State is PetState.JumpCharge or PetState.Jump or PetState.Apex or PetState.Fall); i++)
            engine.Tick(pet, .01);

        Assert.Equal(PetState.Idle, pet.State);
        Assert.Equal(600, pet.FootPosition.Y, 3);
        Assert.InRange(pet.FootPosition.X, 320, 480);
    }

    [Fact]
    public void WindowTrajectoryKeepsHorizontalSpeedWithinWalkCapAndRejectsUnreachableTarget()
    {
        using var definition = TestSpriteFactory.LoadSpriteOnly("window-trajectory-bounds");
        var surface = new World { WindowLandingEnabled = true };
        // Height 520 is now intentionally reachable. A target this far away
        // still cannot be reached under the unchanged horizontal speed cap.
        surface.JumpCandidates = [new("too-far", new(1100, 200, 1250, 280), true, false, false, false)];
        var pet = NewWalker(definition, new(300, 720));
        var engine = new PetEngine(surface, new FixedRandom())
        {
            JumpDesire = 1,
            WindowJumpDesire = 1,
            ClimbDesire = 0
        };

        engine.Tick(pet, .01);

        Assert.Equal(PetState.Jump, pet.State);
        Assert.InRange(Math.Abs(pet.Velocity.X), 0, Math.Min(definition.Movement.WalkSpeed, 1000));
        Assert.Equal(-definition.Movement.JumpSpeed, pet.Velocity.Y, 3);
    }

    [Fact]
    public void DisappearingHighWindowLeavesTheActiveChargedArcUntouched()
    {
        using var definition = TestSpriteFactory.LoadSpriteOnly("window-arc-loss");
        var surface = new World { WindowLandingEnabled = true };
        surface.JumpCandidates = [new("high-target", new(300, 600, 500, 680), true, false, false, false)];
        var pet = NewWalker(definition, new(300, 720));
        var engine = new PetEngine(surface, new FixedRandom())
        {
            JumpDesire = 0,
            WindowJumpDesire = 1,
            ClimbDesire = 0
        };

        engine.Tick(pet, .01);
        for (var i = 0; i < 20 && pet.State == PetState.JumpCharge; i++)
            engine.Tick(pet, .1);
        Assert.Equal(PetState.Jump, pet.State);
        var position = pet.FootPosition;
        var velocity = pet.Velocity;
        surface.JumpCandidates = [];

        engine.Tick(pet, .01);

        Assert.Equal(position.X + velocity.X * .01f, pet.FootPosition.X, 2);
        Assert.Equal(velocity.X, pet.Velocity.X, 2);
        for (var i = 0; i < 200 && pet.State != PetState.Idle; i++) engine.Tick(pet, .01);
        Assert.Equal(720, pet.FootPosition.Y, 3);
    }

    [Fact]
    public void WindowJumpDesireZeroKeepsTheOrdinaryJumpPath()
    {
        using var definition = TestSpriteFactory.LoadSpriteOnly("window-desire-zero");
        var surface = new World { WindowLandingEnabled = true };
        surface.JumpCandidates = [new("target", new(300, 710, 500, 800), true, false, false, false)];
        var pet = NewWalker(definition, new(300, 720));
        var engine = new PetEngine(surface, new FixedRandom())
        {
            JumpDesire = 1,
            WindowJumpDesire = 0
        };

        engine.Tick(pet, .01);

        Assert.Equal(PetState.Jump, pet.State);
        // A zero window-jump desire must keep the ordinary jump path. The
        // ordinary jump preserves the walking speed; its eventual surface is
        // selected by normal physics and may be a visible window top.
        Assert.Equal(60f, pet.Velocity.X, 3);
        Assert.Equal(-definition.Movement.JumpSpeed, pet.Velocity.Y, 3);
    }

    [Fact]
    public void WindowJumpDesireOneSelectsReachableWindowOnEveryEligibleAttempt()
    {
        using var definition = TestSpriteFactory.LoadSpriteOnly("window-desire-full");
        var surface = new World { WindowLandingEnabled = true };
        surface.JumpCandidates = [new("target", new(300, 710, 500, 800), true, false, false, false)];
        var pet = NewWalker(definition, new(300, 720));
        var engine = new PetEngine(surface, new FixedRandom())
        {
            JumpDesire = 0,
            WindowJumpDesire = 1
        };

        engine.Tick(pet, .01);

        Assert.Equal(PetState.JumpCharge, pet.State);
        Assert.True(pet.Velocity.X > 0);
        for (var i = 0; i < 200 && pet.State != PetState.Idle; i++) engine.Tick(pet, .01);
        Assert.Equal(710, pet.FootPosition.Y, 3);
    }

    [Fact]
    public void IntermediateWindowJumpDesireUsesTheSampledTargetBranch()
    {
        using var definition = TestSpriteFactory.LoadSpriteOnly("window-desire-half");
        var surface = new World { WindowLandingEnabled = true };
        surface.JumpCandidates = [new("target", new(300, 710, 500, 800), true, false, false, false)];
        var pet = NewWalker(definition, new(300, 720));
        // The first sample seeds the new attempt delay; the second sample is
        // the window-target branch, and the third allows the ordinary jump.
        var engine = new PetEngine(surface, new SequenceRandom(0, 0.75, 0))
        {
            JumpDesire = 1,
            WindowJumpDesire = 0.5f
        };

        engine.Tick(pet, .01);

        Assert.Equal(PetState.Jump, pet.State);
        Assert.Equal(60f, pet.Velocity.X, 3); // sampled window branch failed; ordinary jump ran
    }

    [Fact]
    public void WindowMovingOrDisappearingDoesNotSnapAnActiveJump()
    {
        using var definition = TestSpriteFactory.LoadSpriteOnly("window-lost");
        var surface = new World { WindowLandingEnabled = true };
        surface.JumpCandidates = [new("target", new(300, 680, 500, 800), true, false, false, false)];
        var pet = NewWalker(definition, new(300, 720));
        var engine = new PetEngine(surface, new FixedRandom()) { JumpDesire = 1, WindowJumpDesire = 1 };
        engine.Tick(pet, .01);
        for (var i = 0; i < 10 && pet.State == PetState.JumpCharge; i++) engine.Tick(pet, .1);
        Assert.Equal(PetState.Jump, pet.State);
        var start = pet.FootPosition;
        var velocity = pet.Velocity;
        surface.JumpCandidates = [];
        engine.Tick(pet, .1);
        Assert.Equal(start.X + velocity.X * .1f, pet.FootPosition.X, 3);
        Assert.Equal(velocity.X, pet.Velocity.X);
        for (var i = 0; i < 100 && pet.State != PetState.Idle; i++) engine.Tick(pet, .1);
        Assert.Equal(720, pet.FootPosition.Y, 3);
    }

    [Fact]
    public void ScaleChangeReleasesAttachmentAndAirbornePetCannotBeFed()
    {
        using var definition = TestSpriteFactory.LoadSpriteOnly("attached");
        var surface = new World();
        var pet = NewWalker(definition, new(1259, 720));
        var engine = new PetEngine(surface, new FixedRandom()) { ClimbDesire = 1, JumpDesire = 0 };
        engine.Tick(pet, .1);
        Assert.Equal(PetState.Climb, pet.State);
        Assert.False(pet.Feed());
        pet.ScaleOverride = 2;
        engine.Tick(pet, .1);
        Assert.Equal(PetState.Fall, pet.State);
    }

    [Fact]
    public void ZeroDesireDoesNotInterruptAnExistingClimbAndDragHasPriority()
    {
        using var definition = TestSpriteFactory.LoadSpriteOnly("zero-desire");
        var pet = NewWalker(definition, new(1259, 720));
        var engine = new PetEngine(new World(), new FixedRandom()) { ClimbDesire = 1 };
        engine.Tick(pet, .1);
        engine.ClimbDesire = 0;
        engine.JumpDesire = 0;
        engine.DefaultWalkEnabled = false;
        engine.Tick(pet, .1);
        Assert.Equal(PetState.Climb, pet.State);
        engine.BeginDrag(pet);
        var position = pet.FootPosition;
        engine.Tick(pet, 100);
        Assert.Equal(PetState.Dragged, pet.State);
        Assert.Equal(position, pet.FootPosition);
    }

    [Fact]
    public void TopWalkReleasesAtOtherEndWithoutCrossingDisplaySeam()
    {
        using var definition = TestSpriteFactory.LoadSpriteOnly("top-walk");
        var pet = NewWalker(definition, new(1259, 720));
        var engine = new PetEngine(new World(), new FixedRandom()) { ClimbDesire = 1, JumpDesire = 0 };
        engine.Tick(pet, .1);
        for (var i = 0; i < 150 && pet.State == PetState.Climb; i++) engine.Tick(pet, .1);
        Assert.Equal(PetState.TopWalk, pet.State);
        Assert.Equal(FacingDirection.Left, pet.Facing);
        pet.TopDropDelay = 1000;
        pet.FootPosition = new(21, pet.ClimbTopY);
        engine.Tick(pet, .1);
        Assert.Equal(PetState.Fall, pet.State);
        Assert.Equal(20, pet.FootPosition.X, 3);
        Assert.Equal(Vector2.Zero, pet.Velocity);
    }

    [Fact]
    public void InvalidDesireOverrideFallsBackToInheritanceAndClampsFiniteValues()
    {
        var behavior = new PetBehaviorOverrides(JumpDesireOverride: float.NaN, ClimbDesireOverride: float.PositiveInfinity);
        Assert.Null(behavior.JumpDesireOverride);
        Assert.Null(behavior.ClimbDesireOverride);
        Assert.Equal(.6f, behavior.ResolveJumpDesire(.6f));
        Assert.Equal(1, (behavior with { JumpDesireOverride = 5 }).ResolveJumpDesire(.6f));
    }

    [Fact]
    public void WindowJumpDesireOverrideInheritsClampsAndResets()
    {
        var behavior = new PetBehaviorOverrides(WindowJumpDesireOverride: 2f);

        Assert.Equal(1f, behavior.ResolveWindowJumpDesire(.2f));
        Assert.Equal(.2f, (behavior with { WindowJumpDesireOverride = null }).ResolveWindowJumpDesire(.2f));
        Assert.Null(new PetBehaviorOverrides(WindowJumpDesireOverride: float.NaN).WindowJumpDesireOverride);
    }

    private static PetInstance NewWalker(YimyWalk.Core.Characters.CharacterDefinition definition, Vector2 foot)
    {
        var pet = new PetInstance(definition, foot) { AutonomousTimersInitialized = true };
        pet.StartWalking(FacingDirection.Right);
        return pet;
    }

    private sealed class World : IWorldSurface
    {
        public WorldRect WorkingArea => new(0, 0, 1280, 720);
        public bool IsValid => true;
        public IDisplayProvider DisplayProvider { get; } = new DisplaySnapshot([
            new(new("D1"), new(0, 0, 1280, 720), new(0, 0, 1280, 720), 1, true),
            new(new("D2"), new(-1280, 0, 0, 720), new(-1280, 0, 0, 720), 1, false)]);
        public bool WindowLandingEnabled { get; set; }
        public IReadOnlyList<WindowSurfaceCandidate> JumpCandidates { get; set; } = [];
        public SurfaceSelection SurfaceForPet(Vector2 start, float halfWidth, Vector2 end)
            => SurfaceSelector.Select(WindowLandingEnabled ? JumpCandidates : [],
                DisplayProvider.Resolve(end)?.WorkingArea ?? WorkingArea, end.X - halfWidth, end.X + halfWidth, start.Y, end.Y);
    }

    private sealed class FixedRandom : IRandomSource
    {
        public double NextDouble() => 0;
        public int Next(int minInclusive, int maxExclusive) => minInclusive;
    }

    private sealed class SequenceRandom(params double[] values) : IRandomSource
    {
        private readonly Queue<double> _values = new(values);
        public double NextDouble() => _values.Count > 0 ? _values.Dequeue() : 0;
        public int Next(int minInclusive, int maxExclusive) => minInclusive;
    }
}
