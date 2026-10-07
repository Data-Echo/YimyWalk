using System.Numerics;
using YimyWalk.Core.Characters;

namespace YimyWalk.Core.Simulation;

public sealed class PetInstance
{
    public const float MinScale = 0.1f;
    public const float MaxScale = 8f;

    private float? _scaleOverride;

    public PetInstance(CharacterDefinition definition, Vector2 footPosition, Guid? instanceId = null)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        InstanceId = instanceId ?? Guid.NewGuid();
        FootPosition = footPosition;
        State = PetState.Fall;
        Facing = FacingDirection.Right;
    }

    public Guid InstanceId { get; }
    public CharacterDefinition Definition { get; }
    public Vector2 FootPosition { get; internal set; }
    public Vector2 Velocity { get; internal set; }
    public FacingDirection Facing { get; internal set; }
    public PetState State { get; internal set; }
    public float StateTime { get; internal set; }
    public float AnimationTime { get; internal set; }
    public bool IsDragging => State == PetState.Dragged;
    public float Hunger { get; private set; }
    public float Boredom { get; private set; }
    public PetBehaviorOverrides BehaviorOverrides { get; set; } = PetBehaviorOverrides.Inherit;

    internal bool AutonomousTimersInitialized { get; set; }
    internal float JumpAttemptDelay { get; set; }
    internal float JumpChargeElapsed { get; set; }
    internal float JumpChargeDuration { get; set; }
    internal float JumpChargeLaunchSpeed { get; set; }
    internal float JumpChargeMaxLaunchSpeed { get; set; }
    internal float ClimbAttemptDelay { get; set; }
    internal bool ClimbEdgeArmed { get; set; } = true;
    internal string? AttachedDisplayId { get; set; }
    internal WorldRect AttachedDisplayArea { get; set; }
    internal float AttachedDisplayScale { get; set; } = 1f;
    internal float AttachedEffectiveScale { get; set; } = 1f;
    internal string? LastClimbAttemptDisplayId { get; set; }
    internal FacingDirection ClimbSide { get; set; }
    internal float ClimbTopY { get; set; }
    internal float ClimbEdgeX { get; set; }
    internal float TopDropDelay { get; set; }

    /// <summary>
    /// Runtime-only display multiplier. Null means this instance follows the
    /// application-wide display scale; it is deliberately not part of settings
    /// persistence or the immutable character definition.
    /// </summary>
    public float? ScaleOverride
    {
        get => _scaleOverride;
        set => _scaleOverride = value is { } custom && float.IsFinite(custom)
            ? Math.Clamp(custom, MinScale, MaxScale)
            : null;
    }

    public float ResolveScale(float globalScale)
    {
        var global = float.IsFinite(globalScale)
            ? Math.Clamp(globalScale, MinScale, MaxScale)
            : 1f;
        return ScaleOverride ?? global;
    }

    public void StartWalking(FacingDirection direction)
    {
        Facing = direction;
        State = PetState.Walk;
        StateTime = 0;
        Velocity = Vector2.Zero;
    }

    public void StartJump()
    {
        ClearJumpCharge();
        State = PetState.Jump;
        StateTime = 0;
        Velocity = new Vector2(Velocity.X, -Definition.Movement.JumpSpeed);
    }

    internal void StartJumpCharge(float duration, float launchSpeed, float maxLaunchSpeed = 0)
    {
        JumpChargeElapsed = 0;
        JumpChargeDuration = float.IsFinite(duration) ? Math.Max(0, duration) : 0;
        JumpChargeLaunchSpeed = float.IsFinite(launchSpeed) ? Math.Max(0, launchSpeed) : Definition.Movement.JumpSpeed;
        JumpChargeMaxLaunchSpeed = float.IsFinite(maxLaunchSpeed) ? Math.Max(0, maxLaunchSpeed) : 0;
        State = PetState.JumpCharge;
        StateTime = 0;
        Velocity = new Vector2(Velocity.X, 0);
    }

    internal void ClearJumpCharge()
    {
        JumpChargeElapsed = 0;
        JumpChargeDuration = 0;
        JumpChargeLaunchSpeed = 0;
        JumpChargeMaxLaunchSpeed = 0;
    }

    public bool Feed(float amount = 0.35f)
    {
        if (State is not (PetState.Idle or PetState.Walk))
            return false;
        if (!float.IsFinite(amount) || amount <= 0) return false;
        Hunger = Math.Clamp(Hunger - amount, 0f, 1f);
        State = PetState.Eat;
        StateTime = 0;
        Velocity = Vector2.Zero;
        return true;
    }

    internal void AdvanceNeeds(float deltaSeconds, bool hungerEnabled, float needRate)
    {
        var delta = Math.Clamp(deltaSeconds, 0f, (float)PetEngine.MaxDeltaSeconds);
        var rate = Math.Clamp(float.IsFinite(needRate) ? needRate : 1f, 0f, 4f);
        if (hungerEnabled)
            Hunger = Math.Clamp(Hunger + Definition.Defaults.HungerIncreasePerMinute / 60f * delta * rate, 0f, 1f);

        var boredomDelta = State == PetState.Eat ? -0.01f * delta : 0.001f * delta;
        Boredom = Math.Clamp(Boredom + boredomDelta, 0f, 1f);
    }

    public FrameKind GetFrameKind()
        => State switch
        {
            PetState.Walk when Facing == FacingDirection.Left => FrameKind.WalkLeft,
            PetState.Walk => FrameKind.WalkRight,
            PetState.JumpCharge => FrameKind.Apex,
            PetState.Jump => FrameKind.Jump,
            PetState.Apex => FrameKind.Apex,
            PetState.Fall => FrameKind.Fall,
            PetState.Climb => FrameKind.Apex,
            PetState.TopWalk when Facing == FacingDirection.Left => FrameKind.WalkLeft,
            PetState.TopWalk => FrameKind.WalkRight,
            _ => FrameKind.Idle
        };
}
