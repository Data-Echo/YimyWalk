using System.Numerics;
using YimyWalk.Core.Characters;
using YimyWalk.Core.Display;
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
    public void ChargedJumpUsesApexFrameAndDoesNotMoveUntilChargeCompletes()
    {
        using var definition = TestSpriteFactory.LoadSpriteOnly("charged-stationary");
        var pet = new PetInstance(definition, new Vector2(300, 720))
        {
            AutonomousTimersInitialized = true,
            JumpAttemptDelay = 0,
            ClimbAttemptDelay = 999
        };
        pet.StartWalking(FacingDirection.Right);
        var engine = new PetEngine(new Surface(), new SequenceRandom(0, 0, 1, 1)) { JumpDesire = 1, WindowJumpDesire = 0 };

        engine.Tick(pet, 0.01);

        Assert.Equal(PetState.JumpCharge, pet.State);
        Assert.Equal(FrameKind.Apex, pet.GetFrameKind());
        var position = pet.FootPosition;
        var horizontalVelocity = pet.Velocity.X;
        engine.Tick(pet, 0.1);

        Assert.Equal(PetState.JumpCharge, pet.State);
        Assert.Equal(position, pet.FootPosition);
        Assert.Equal(horizontalVelocity, pet.Velocity.X);
        Assert.InRange(pet.JumpChargeElapsed, 0.099f, 0.101f);
    }

    [Fact]
    public void UnchargedJumpStartsImmediatelyAtBaseSpeed()
    {
        using var definition = TestSpriteFactory.LoadSpriteOnly("uncharged");
        var pet = new PetInstance(definition, new Vector2(300, 720))
        {
            AutonomousTimersInitialized = true,
            JumpAttemptDelay = 0,
            ClimbAttemptDelay = 999
        };
        pet.StartWalking(FacingDirection.Right);
        var engine = new PetEngine(new Surface(), new SequenceRandom(0, 0, 0)) { JumpDesire = 1, WindowJumpDesire = 0 };

        engine.Tick(pet, 0.01);

        Assert.Equal(PetState.Jump, pet.State);
        Assert.Equal(-definition.Movement.JumpSpeed, pet.Velocity.Y, 3);
    }

    [Fact]
    public void ChargeDurationRaisesLaunchSpeedWithoutExceedingCap()
    {
        using var definition = TestSpriteFactory.LoadSpriteOnly("charged-speed");
        var pet = new PetInstance(definition, new Vector2(300, 720))
        {
            AutonomousTimersInitialized = true,
            JumpAttemptDelay = 0,
            ClimbAttemptDelay = 999
        };
        pet.StartWalking(FacingDirection.Right);
        var engine = new PetEngine(new Surface(), new SequenceRandom(0, 0, 1, 1)) { JumpDesire = 1, WindowJumpDesire = 0 };

        engine.Tick(pet, 0.01);
        for (var i = 0; i < 10 && pet.State == PetState.JumpCharge; i++)
            engine.Tick(pet, 0.1);

        Assert.Equal(PetState.Jump, pet.State);
        Assert.True(Math.Abs(pet.Velocity.Y) > definition.Movement.JumpSpeed);
        Assert.InRange(Math.Abs(pet.Velocity.Y), definition.Movement.JumpSpeed, definition.Movement.JumpSpeed * 1.75f);
    }

    [Fact]
    public void DifferentChargeDurationsProduceDifferentLaunchSpeeds()
    {
        using var definition = TestSpriteFactory.LoadSpriteOnly("charge-duration-range");
        var shortPet = new PetInstance(definition, new Vector2(300, 720))
        {
            AutonomousTimersInitialized = true,
            JumpAttemptDelay = 0,
            ClimbAttemptDelay = 999
        };
        var longPet = new PetInstance(definition, new Vector2(300, 720))
        {
            AutonomousTimersInitialized = true,
            JumpAttemptDelay = 0,
            ClimbAttemptDelay = 999
        };
        shortPet.StartWalking(FacingDirection.Right);
        longPet.StartWalking(FacingDirection.Right);
        var shortEngine = new PetEngine(new Surface(), new SequenceRandom(0, 0, 1, 0)) { JumpDesire = 1, WindowJumpDesire = 0 };
        var longEngine = new PetEngine(new Surface(), new SequenceRandom(0, 0, 1, 1)) { JumpDesire = 1, WindowJumpDesire = 0 };

        shortEngine.Tick(shortPet, 0.01);
        longEngine.Tick(longPet, 0.01);
        shortEngine.Tick(shortPet, 0.1);
        shortEngine.Tick(shortPet, 0.04);
        shortEngine.Tick(shortPet, 0.01);
        for (var i = 0; i < 4; i++)
            longEngine.Tick(longPet, 0.1);
        longEngine.Tick(longPet, 0.05);

        Assert.Equal(PetState.Jump, shortPet.State);
        Assert.Equal(PetState.Jump, longPet.State);
        Assert.True(Math.Abs(longPet.Velocity.Y) > Math.Abs(shortPet.Velocity.Y));
        Assert.InRange(Math.Abs(longPet.Velocity.Y), definition.Movement.JumpSpeed, definition.Movement.JumpSpeed * 1.75f);
    }

    [Fact]
    public void ZeroJumpDesireDoesNotStartAChargeOrJump()
    {
        using var definition = TestSpriteFactory.LoadSpriteOnly("zero-charge");
        var pet = new PetInstance(definition, new Vector2(300, 720))
        {
            AutonomousTimersInitialized = true,
            JumpAttemptDelay = 0,
            ClimbAttemptDelay = 999
        };
        pet.StartWalking(FacingDirection.Right);
        var engine = new PetEngine(new Surface(), new SequenceRandom(0, 1, 1)) { JumpDesire = 0, WindowJumpDesire = 0 };

        engine.Tick(pet, 0.01);

        Assert.Equal(PetState.Walk, pet.State);
    }

    [Fact]
    public void PausedChargeKeepsItsElapsedTimeFrozen()
    {
        using var definition = TestSpriteFactory.LoadSpriteOnly("paused-charge");
        using var session = new PetSession(new Surface(), new SequenceRandom());
        session.RegisterDefinition(definition.Id, definition);
        var pet = session.Create(definition.Id, new Vector2(300, 720));
        pet.State = PetState.JumpCharge;
        pet.JumpChargeDuration = 0.45f;
        pet.JumpChargeLaunchSpeed = definition.Movement.JumpSpeed * 1.5f;
        pet.Velocity = new Vector2(60, 0);

        Assert.Equal(PetState.JumpCharge, pet.State);
        var elapsed = pet.JumpChargeElapsed;
        session.Pause();
        session.Tick(0.1);
        Assert.Equal(elapsed, pet.JumpChargeElapsed);
        session.Resume();
        session.Tick(0.1);
        Assert.True(pet.JumpChargeElapsed > elapsed);
    }

    [Fact]
    public void BallisticPlannerUsesDescendingIntersectionAndRejectsUnreachableHeight()
    {
        Assert.True(BallisticMotion.TryGetDescendingTime(720, 720, 450, 1500, 1500, out var time));
        Assert.InRange(time, 0.59f, 0.61f);
        Assert.False(BallisticMotion.TryGetDescendingTime(720, 600, 450, 1500, 1500, out _));
    }

    [Fact]
    public void JumpPlannerRequiresReachableDescendingLandingInterval()
    {
        var candidates = new[]
        {
            new WindowSurfaceCandidate("reachable", new WorldRect(320, 700, 500, 800), true, false, false, false),
            new WindowSurfaceCandidate("too-high", new WorldRect(520, 650, 700, 800), true, false, false, false),
            new WindowSurfaceCandidate("too-narrow", new WorldRect(305, 700, 312, 800), true, false, false, false)
        };

        Assert.True(JumpTargetPlanner.TrySelect(candidates, new Vector2(300, 720), FacingDirection.Right,
            60, 450, 1500, 1500, 5, out var target));
        Assert.Equal("reachable", target.SourceId);
        Assert.True(target.LandingTime > 0);
        Assert.InRange(target.HorizontalVelocity, 0, 60);
    }

    [Fact]
    public void ClimbStartsAtDisplayEdgeAndTopReleaseFallsNormally()
    {
        using var definition = TestSpriteFactory.LoadSpriteOnly("climber");
        var display = new DisplayInfo(new DisplayId("D1"), new WorldRect(0, 0, 100, 100), new WorldRect(0, 0, 100, 100), 1f, true);
        var surface = new DisplaySurface(new DisplaySnapshot(new[] { display }));
        var pet = new PetInstance(definition, new Vector2(79, 100)) { AutonomousTimersInitialized = true, ClimbAttemptDelay = 0 };
        pet.StartWalking(FacingDirection.Right);
        var engine = new PetEngine(surface, new Random()) { ClimbDesire = 1 };

        engine.Tick(pet, 0.1);
        Assert.Equal(PetState.Climb, pet.State);
        for (var i = 0; i < 30 && pet.State == PetState.Climb; i++) engine.Tick(pet, 0.1);
        Assert.Equal(PetState.TopWalk, pet.State);
        pet.TopDropDelay = 0;
        engine.Tick(pet, 0.1);
        Assert.Equal(PetState.Fall, pet.State);
    }

    [Fact]
    public void ClimbUsesApexFrameWhileTopWalkKeepsDirectionalWalkFrame()
    {
        using var definition = TestSpriteFactory.LoadSpriteOnly("climb-frame");
        var pet = new PetInstance(definition, new Vector2(20, 100));

        pet.State = PetState.Climb;
        Assert.Equal(FrameKind.Apex, pet.GetFrameKind());

        pet.State = PetState.TopWalk;
        pet.Facing = FacingDirection.Left;
        Assert.Equal(FrameKind.WalkLeft, pet.GetFrameKind());
    }

    [Fact]
    public void FailedAutonomousAttemptArmsOneNewWaitInsteadOfRetryingNextTick()
    {
        using var definition = TestSpriteFactory.LoadSpriteOnly("attempt-wait");
        var pet = new PetInstance(definition, new Vector2(300, 720))
        {
            AutonomousTimersInitialized = true,
            JumpAttemptDelay = 0,
            ClimbAttemptDelay = 999
        };
        pet.StartWalking(FacingDirection.Right);
        var engine = new PetEngine(new Surface(), new SequenceRandom(0, 0.9)) { JumpDesire = 0.5f };

        engine.Tick(pet, 0.01);
        Assert.Equal(PetState.Walk, pet.State);
        Assert.InRange(pet.JumpAttemptDelay, 7.99f, 8.01f);
        engine.Tick(pet, 0.01);
        Assert.Equal(PetState.Walk, pet.State);
        Assert.True(pet.JumpAttemptDelay > 7.9f);
    }

    [Fact]
    public void PausingFreezesAutonomousWaitWithoutReplayingMissedTime()
    {
        using var definition = TestSpriteFactory.LoadSpriteOnly("pause-wait");
        using var session = new PetSession(new Surface(), new SequenceRandom(0));
        session.RegisterDefinition(definition.Id, definition);
        var pet = session.Create(definition.Id, new Vector2(300, 720));
        pet.StartWalking(FacingDirection.Right);
        session.Tick(0.01);
        var stateTime = pet.StateTime;
        var delay = pet.JumpAttemptDelay;

        session.Pause();
        session.Tick(100);
        session.Resume();
        Assert.Equal(stateTime, pet.StateTime);
        Assert.Equal(delay, pet.JumpAttemptDelay);
        session.Tick(0.01);
        Assert.Equal(PetState.Walk, pet.State);
        Assert.True(pet.JumpAttemptDelay > delay - 0.02f);
    }

    [Fact]
    public void FailedEdgeClimbIsDeduplicatedUntilAnchorLeavesSafeEdgeByTwelvePixels()
    {
        using var definition = TestSpriteFactory.LoadSpriteOnly("edge-wait");
        var display = new DisplayInfo(new DisplayId("D1"), new WorldRect(0, 0, 200, 200), new WorldRect(0, 0, 200, 200), 1f, true);
        var surface = new DisplaySurface(new DisplaySnapshot(new[] { display }));
        var pet = new PetInstance(definition, new Vector2(179, 200))
        {
            AutonomousTimersInitialized = true,
            JumpAttemptDelay = 999,
            ClimbAttemptDelay = 0
        };
        pet.StartWalking(FacingDirection.Right);
        var engine = new PetEngine(surface, new SequenceRandom(0, 0.9)) { ClimbDesire = 0.5f };

        engine.Tick(pet, 0.1);
        Assert.Equal(PetState.Walk, pet.State);
        Assert.False(pet.ClimbEdgeArmed);
        pet.ClimbAttemptDelay = 0;
        pet.Facing = FacingDirection.Left;
        engine.Tick(pet, 0.1);
        Assert.False(pet.ClimbEdgeArmed);
        Assert.Equal(PetState.Walk, pet.State);

        pet.FootPosition = new Vector2(160, 200);
        engine.Tick(pet, 0.01);
        Assert.True(pet.ClimbEdgeArmed);
    }

    [Fact]
    public void LeftEdgeClimbReversesTopWalkIntoDisplay()
    {
        using var definition = TestSpriteFactory.LoadSpriteOnly("left-climber");
        var display = new DisplayInfo(new DisplayId("D1"), new WorldRect(0, 0, 200, 200), new WorldRect(0, 0, 200, 200), 1f, true);
        var surface = new DisplaySurface(new DisplaySnapshot(new[] { display }));
        var pet = new PetInstance(definition, new Vector2(21, 200))
        {
            AutonomousTimersInitialized = true,
            JumpAttemptDelay = 999,
            ClimbAttemptDelay = 0
        };
        pet.StartWalking(FacingDirection.Left);
        var engine = new PetEngine(surface, new SequenceRandom(0)) { ClimbDesire = 1 };

        engine.Tick(pet, 0.1);
        Assert.Equal(PetState.Climb, pet.State);
        for (var i = 0; i < 40 && pet.State == PetState.Climb; i++) engine.Tick(pet, 0.1);
        Assert.Equal(PetState.TopWalk, pet.State);
        Assert.Equal(FacingDirection.Right, pet.Facing);
    }

    [Fact]
    public void TopReleaseUsesWindowSurfaceBeforeWorkingAreaFloor()
    {
        using var definition = TestSpriteFactory.LoadSpriteOnly("top-window");
        var display = new DisplayInfo(new DisplayId("D1"), new WorldRect(0, 0, 240, 240), new WorldRect(0, 0, 240, 240), 1f, true);
        var surface = new MutableSurface(new DisplaySnapshot(new[] { display }))
        {
            WindowLandingEnabled = true,
            JumpCandidates = new[] { new WindowSurfaceCandidate("window", new WorldRect(80, 110, 240, 180), true, false, false, false) }
        };
        var pet = new PetInstance(definition, new Vector2(219, 240))
        {
            AutonomousTimersInitialized = true,
            JumpAttemptDelay = 999,
            ClimbAttemptDelay = 0
        };
        pet.StartWalking(FacingDirection.Right);
        var engine = new PetEngine(surface, new SequenceRandom(0)) { ClimbDesire = 1 };

        engine.Tick(pet, 0.1);
        Assert.Equal(PetState.Climb, pet.State);
        for (var i = 0; i < 40 && pet.State == PetState.Climb; i++) engine.Tick(pet, 0.1);
        Assert.Equal(PetState.TopWalk, pet.State);
        pet.TopDropDelay = 0;
        for (var i = 0; i < 40 && pet.State != PetState.Idle; i++) engine.Tick(pet, 0.1);
        Assert.Equal(PetState.Idle, pet.State);
        Assert.Equal(110f, pet.FootPosition.Y);
    }

    [Fact]
    public void AttachmentTopologyChangeFallsWithoutKeepingStaleTopPosition()
    {
        using var definition = TestSpriteFactory.LoadSpriteOnly("top-topology");
        var first = new DisplayInfo(new DisplayId("D1"), new WorldRect(0, 0, 240, 240), new WorldRect(0, 0, 240, 240), 1f, true);
        var second = new DisplayInfo(new DisplayId("D2"), new WorldRect(0, 0, 240, 240), new WorldRect(0, 0, 240, 240), 1.25f, true);
        var surface = new MutableSurface(new DisplaySnapshot(new[] { first }));
        var pet = new PetInstance(definition, new Vector2(219, 240))
        {
            AutonomousTimersInitialized = true,
            JumpAttemptDelay = 999,
            ClimbAttemptDelay = 0
        };
        pet.StartWalking(FacingDirection.Right);
        var engine = new PetEngine(surface, new SequenceRandom(0)) { ClimbDesire = 1 };

        engine.Tick(pet, 0.1);
        for (var i = 0; i < 40 && pet.State == PetState.Climb; i++) engine.Tick(pet, 0.1);
        Assert.Equal(PetState.TopWalk, pet.State);
        surface.Provider = new DisplaySnapshot(new[] { second });
        engine.Tick(pet, 0.1);
        Assert.Equal(PetState.Fall, pet.State);
        Assert.Equal(Vector2.Zero, pet.Velocity);
    }

    private sealed class Surface : IWorldSurface
    {
        public WorldRect WorkingArea => new(0, 0, 1280, 720);
        public bool IsValid => true;
    }
    private sealed class DisplaySurface(DisplaySnapshot snapshot) : IWorldSurface
    {
        public WorldRect WorkingArea => snapshot.Primary?.WorkingArea ?? new WorldRect(0, 0, 100, 100);
        public bool IsValid => true;
        public IDisplayProvider DisplayProvider { get; } = snapshot;
    }
    private sealed class MutableSurface(DisplaySnapshot snapshot) : IWorldSurface
    {
        public IDisplayProvider Provider { get; set; } = snapshot;
        public WorldRect WorkingArea => Provider.Primary?.WorkingArea ?? new WorldRect(0, 0, 240, 240);
        public bool IsValid => true;
        public IDisplayProvider DisplayProvider => Provider;
        public bool WindowLandingEnabled { get; set; }
        public IReadOnlyList<WindowSurfaceCandidate> JumpCandidates { get; set; } = Array.Empty<WindowSurfaceCandidate>();
        public SurfaceSelection SurfaceForPet(Vector2 footPosition, float footHalfWidth, Vector2 nextFootPosition)
            => SurfaceSelector.Select(JumpCandidates, Provider.Resolve(nextFootPosition)?.WorkingArea ?? WorkingArea, nextFootPosition.X - footHalfWidth,
                nextFootPosition.X + footHalfWidth, footPosition.Y, nextFootPosition.Y);
    }
    private sealed class Random : IRandomSource
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
