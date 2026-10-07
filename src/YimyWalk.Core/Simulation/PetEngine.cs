using System.Numerics;
using YimyWalk.Core.Display;

namespace YimyWalk.Core.Simulation;

public sealed class PetEngine
{
    public const float MaxTossSpeed = 1200f;
    public const double MaxDeltaSeconds = 0.1;
    public const float InitialJumpDelayMin = 8f;
    public const float InitialJumpDelayMax = 16f;
    public const float InitialClimbDelayMin = 10f;
    public const float InitialClimbDelayMax = 20f;
    public const float ClimbEdgeRearmDistance = 12f;
    public const float TopDropDelayMin = 6f;
    public const float TopDropDelayMax = 12f;
    public const float JumpChargeDurationMin = 0.15f;
    public const float JumpChargeDurationMax = 0.45f;
    public const float ChargedJumpSpeedMultiplier = 1.75f;
    // Ordinary attention-seeking jumps keep the 1.75x cap. Window jumps are
    // an explicit target action and get a separate finite ceiling so a normal
    // desktop-height window can be reached without making every jump huge.
    public const float WindowJumpSpeedMultiplier = 6f;

    private readonly IRandomSource _random;
    public bool HungerEnabled { get; set; } = true;
    public float NeedRate { get; set; } = 1f;
    public bool DefaultWalkEnabled { get; set; } = true;
    public float ActivityRate { get; set; } = 1f;
    public float JumpDesire { get; set; } = 0.2f;
    public float ClimbDesire { get; set; } = 0.12f;
    public float WindowJumpDesire { get; set; } = 0.2f;
    public float GlobalScale { get; set; } = 1f;

    public PetEngine(IWorldSurface surface, IRandomSource random)
    {
        Surface = surface ?? throw new ArgumentNullException(nameof(surface));
        _random = random ?? throw new ArgumentNullException(nameof(random));
    }

    public IWorldSurface Surface { get; }

    public void Tick(IEnumerable<PetInstance> pets, double deltaSeconds)
    {
        ArgumentNullException.ThrowIfNull(pets);
        foreach (var pet in pets)
            Tick(pet, deltaSeconds);
    }

    public void Tick(PetInstance pet, double deltaSeconds)
    {
        ArgumentNullException.ThrowIfNull(pet);
        var delta = (float)(double.IsFinite(deltaSeconds) ? Math.Clamp(deltaSeconds, 0, MaxDeltaSeconds) : 0);
        if (delta <= 0) return;
        pet.AdvanceNeeds(delta, HungerEnabled, NeedRate);
        if (!Surface.IsValid)
        {
            RelocateToSurface(pet, Surface.WorkingAreaForPoint(pet.FootPosition));
            return;
        }

        var travelBounds = Surface.HorizontalTravelBounds;
        InitializeAutonomousTimers(pet);
        if (pet.State is PetState.Climb or PetState.TopWalk && !ValidateAttachment(pet))
        {
            // Only a removed/rearranged display may require relocation. A
            // scale change in the same work area simply releases the anchor.
            pet.FootPosition = Surface.WorkingAreaForPoint(pet.FootPosition).ClampFoot(pet.FootPosition);
            TransitionToFall(pet);
            return;
        }
        var nextFootPosition = GetNextFootPosition(pet, delta, travelBounds);
        var halfWidth = pet.State is PetState.JumpCharge or PetState.Jump or PetState.Apex or PetState.Fall
            ? Math.Max(1f, pet.Definition.CellWidth * EffectiveScale(pet) / 2f)
            : 0f;
        var queryStart = pet.FootPosition;
        if (pet.State is PetState.Jump or PetState.Apex && pet.Velocity.Y < 0)
        {
            var toApex = -pet.Velocity.Y / pet.Definition.Movement.Gravity;
            if (toApex < delta)
                queryStart = new Vector2(WrapHorizontal(queryStart.X + pet.Velocity.X * toApex, travelBounds),
                    queryStart.Y - pet.Velocity.Y * pet.Velocity.Y / (2 * pet.Definition.Movement.Gravity));
        }
        var selectedSurface = pet.State is PetState.Climb or PetState.TopWalk
            ? new SurfaceSelection(pet.AttachedDisplayArea, IsFallback: true, SourceId: pet.AttachedDisplayId)
            : Surface.SurfaceForPet(queryStart, halfWidth, nextFootPosition);
        var area = selectedSurface.Area;
        if ((pet.State is PetState.Idle or PetState.Walk or PetState.Eat or PetState.JumpCharge)
            && selectedSurface.IsFallback
            && pet.FootPosition.Y < area.Bottom - 0.5f)
        {
            if (pet.State == PetState.Walk)
            {
                var walkSpeed = Math.Clamp(pet.Definition.Movement.WalkSpeed, 0, 1000);
                pet.Velocity = new Vector2((int)pet.Facing * walkSpeed, pet.Velocity.Y);
            }
            pet.State = PetState.Fall;
            pet.StateTime = 0;
            pet.ClearJumpCharge();
        }
        pet.StateTime += delta;
        pet.AnimationTime += delta;

        if (pet.State == PetState.Dragged)
        {
            pet.Velocity = Vector2.Zero;
            return;
        }

        if (pet.State == PetState.Walk && !pet.BehaviorOverrides.ResolveWalkEnabled(DefaultWalkEnabled))
        {
            pet.State = PetState.Idle;
            pet.StateTime = 0;
            pet.Velocity = Vector2.Zero;
        }

        if (pet.State is PetState.Idle or PetState.Walk)
        {
            var rate = pet.BehaviorOverrides.ResolveActivityRate(ActivityRate);
            pet.JumpAttemptDelay = Math.Max(0, pet.JumpAttemptDelay - delta * rate);
            pet.ClimbAttemptDelay = Math.Max(0, pet.ClimbAttemptDelay - delta * rate);
            RearmClimbEdge(pet);
        }

        switch (pet.State)
        {
            case PetState.JumpCharge:
                IntegrateJumpCharge(pet, area, travelBounds, delta);
                break;
            case PetState.Jump:
                IntegrateAirborne(pet, area, travelBounds, delta);
                if (pet.State == PetState.Jump && Math.Abs(pet.Velocity.Y) <= 25f)
                    pet.State = PetState.Apex;
                else if (pet.State == PetState.Jump && pet.Velocity.Y > 25f)
                    pet.State = PetState.Fall;
                break;
            case PetState.Apex:
                IntegrateAirborne(pet, area, travelBounds, delta);
                if (pet.State == PetState.Apex && pet.Velocity.Y > 25f)
                    pet.State = PetState.Fall;
                break;
            case PetState.Fall:
                IntegrateAirborne(pet, area, travelBounds, delta);
                break;
            case PetState.Walk:
                if (TryStartClimb(pet, area, delta))
                    break;
                IntegrateWalking(pet, area, Surface.HorizontalTravelBounds, delta);
                TryAutonomousJump(pet, selectedSurface.SourceId);
                break;
            case PetState.Idle:
                pet.Velocity = Vector2.Zero;
                var activityRate = pet.BehaviorOverrides.ResolveActivityRate(ActivityRate);
                var idleMin = Math.Max(0, pet.Definition.Defaults.IdleSecondsMin) / activityRate;
                var idleMax = Math.Max(idleMin, pet.Definition.Defaults.IdleSecondsMax) / activityRate;
                if (pet.BehaviorOverrides.ResolveWalkEnabled(DefaultWalkEnabled)
                    && (pet.StateTime >= idleMax
                        || (pet.StateTime >= idleMin && _random.NextDouble() >= 0.25)))
                {
                    // The random choice is deliberately infrequent and deterministic
                    // under a test source; the third row is never an animation loop.
                    pet.StartWalking(_random.Next(0, 2) == 0 ? FacingDirection.Left : FacingDirection.Right);
                }
                break;
            case PetState.Eat:
                pet.Velocity = Vector2.Zero;
                if (pet.StateTime >= 1f)
                {
                    pet.State = PetState.Idle;
                    pet.StateTime = 0;
                }
                break;
            case PetState.Climb:
                IntegrateClimbing(pet, delta);
                break;
            case PetState.TopWalk:
                IntegrateTopWalking(pet, delta);
                break;
        }
    }

    public void BeginDrag(PetInstance pet)
    {
        ArgumentNullException.ThrowIfNull(pet);
        pet.State = PetState.Dragged;
        pet.StateTime = 0;
        pet.Velocity = Vector2.Zero;
        pet.ClearJumpCharge();
        pet.AttachedDisplayId = null;
        ResetAutonomousTimers(pet);
    }

    public void SetDraggedFootPosition(PetInstance pet, Vector2 footPosition)
    {
        ArgumentNullException.ThrowIfNull(pet);
        if (pet.State != PetState.Dragged)
            BeginDrag(pet);
        pet.FootPosition = Surface.IsValid ? Surface.WorkingAreaForPoint(footPosition).ClampFoot(footPosition) : footPosition;
    }

    public void ReleaseDrag(PetInstance pet, Vector2 tossVelocity)
    {
        ArgumentNullException.ThrowIfNull(pet);
        pet.Velocity = new Vector2(
            Math.Clamp(tossVelocity.X, -MaxTossSpeed, MaxTossSpeed),
            Math.Clamp(tossVelocity.Y, -MaxTossSpeed, MaxTossSpeed));
        pet.ClearJumpCharge();
        pet.State = pet.Velocity.Y < -25f ? PetState.Jump : PetState.Fall;
        pet.StateTime = 0;
    }

    private static void IntegrateWalking(PetInstance pet, WorldRect area, WorldRect travelBounds, float delta)
    {
        var speed = Math.Clamp(pet.Definition.Movement.WalkSpeed, 0, 1000);
        var x = pet.FootPosition.X + (int)pet.Facing * speed * delta;
        var width = travelBounds.Right - travelBounds.Left;
        if (width > 0)
        {
            while (x < travelBounds.Left) x += width;
            while (x > travelBounds.Right) x -= width;
        }
        else
            x = Math.Clamp(x, area.Left, area.Right);

        pet.FootPosition = new Vector2(x, area.Bottom);
        pet.Velocity = new Vector2((int)pet.Facing * speed, 0);
    }

    private void IntegrateAirborne(PetInstance pet, WorldRect area, WorldRect travelBounds, float delta)
    {
        var movement = pet.Definition.Movement;
        var (displacement, vy) = BallisticMotion.Advance(pet.Velocity.Y, movement.Gravity, movement.MaxFallSpeed, delta);
        var next = new Vector2(
            WrapHorizontal(pet.FootPosition.X + pet.Velocity.X * delta, travelBounds),
            pet.FootPosition.Y + displacement);
        if (vy >= 0 && next.Y >= area.Bottom - 0.001f)
        {
            // Stop horizontal travel at the actual descending intersection,
            // rather than consuming the rest of the Tick beyond the ledge.
            if (BallisticMotion.TryGetDescendingTimeFromVelocity(pet.FootPosition.Y, area.Bottom,
                    pet.Velocity.Y, movement.Gravity, movement.MaxFallSpeed, out var landingTime)
                && landingTime <= delta + 0.001f)
                next.X = WrapHorizontal(pet.FootPosition.X + pet.Velocity.X * Math.Min(delta, landingTime), travelBounds);
            pet.FootPosition = new Vector2(
                travelBounds.Width > 0 ? next.X : Math.Clamp(next.X, area.Left, area.Right),
                area.Bottom);
            pet.Velocity = Vector2.Zero;
            pet.State = PetState.Idle;
            pet.StateTime = 0;
            pet.ClearJumpCharge();
            ResetAutonomousTimers(pet);
            return;
        }

        pet.Velocity = new Vector2(pet.Velocity.X, vy);
        pet.FootPosition = new Vector2(
            travelBounds.Width > 0 ? next.X : Math.Clamp(next.X, area.Left, area.Right),
            next.Y);
    }

    private void IntegrateJumpCharge(PetInstance pet, WorldRect area, WorldRect travelBounds, float delta)
    {
        // Charging is an intentional pause in horizontal movement. The sampled
        // X velocity is retained so launching never changes the existing speed
        // cap or introduces a second random movement decision.
        pet.Velocity = new Vector2(pet.Velocity.X, 0);
        pet.JumpChargeElapsed += delta;
        if (pet.JumpChargeElapsed + 0.0001f < pet.JumpChargeDuration)
            return;

        var remainder = Math.Max(0, pet.JumpChargeElapsed - pet.JumpChargeDuration);
        var launchSpeed = Math.Clamp(pet.JumpChargeLaunchSpeed,
            0, Math.Max(0, pet.JumpChargeMaxLaunchSpeed > 0
                ? pet.JumpChargeMaxLaunchSpeed
                : pet.Definition.Movement.JumpSpeed * ChargedJumpSpeedMultiplier));
        pet.ClearJumpCharge();
        pet.State = PetState.Jump;
        pet.StateTime = 0;
        pet.Velocity = new Vector2(pet.Velocity.X, -launchSpeed);
        if (remainder > 0.0001f)
            IntegrateAirborne(pet, area, travelBounds, Math.Min(remainder, (float)MaxDeltaSeconds));
    }

    private void BeginJumpCharge(PetInstance pet, float duration)
    {
        var baseSpeed = Math.Max(0, pet.Definition.Movement.JumpSpeed);
        var normalized = (duration - JumpChargeDurationMin)
            / Math.Max(0.0001f, JumpChargeDurationMax - JumpChargeDurationMin);
        var launchSpeed = baseSpeed * (1f + Math.Clamp(normalized, 0, 1) * (ChargedJumpSpeedMultiplier - 1f));
        pet.StartJumpCharge(duration, Math.Min(launchSpeed, baseSpeed * ChargedJumpSpeedMultiplier),
            baseSpeed * ChargedJumpSpeedMultiplier);
    }

    private void BeginPlannedJumpCharge(PetInstance pet, float launchSpeed)
    {
        var baseSpeed = Math.Max(0, pet.Definition.Movement.JumpSpeed);
        var maxSpeed = baseSpeed * WindowJumpSpeedMultiplier;
        var clampedSpeed = Math.Clamp(float.IsFinite(launchSpeed) ? launchSpeed : baseSpeed, baseSpeed, maxSpeed);
        var normalized = maxSpeed <= baseSpeed
            ? 0
            : (clampedSpeed - baseSpeed) / (maxSpeed - baseSpeed);
        var duration = JumpChargeDurationMin
            + Math.Clamp(normalized, 0, 1) * (JumpChargeDurationMax - JumpChargeDurationMin);
        pet.StartJumpCharge(duration, clampedSpeed, maxSpeed);
    }

    private void InitializeAutonomousTimers(PetInstance pet)
    {
        if (pet.AutonomousTimersInitialized) return;
        ResetAutonomousTimers(pet);
    }

    private void ResetAutonomousTimers(PetInstance pet)
    {
        pet.AutonomousTimersInitialized = true;
        pet.JumpAttemptDelay = RandomDelay(InitialJumpDelayMin, InitialJumpDelayMax);
        pet.ClimbAttemptDelay = RandomDelay(InitialClimbDelayMin, InitialClimbDelayMax);
    }

    private float RandomDelay(float minimum, float maximum)
        => minimum + (float)Math.Clamp(_random.NextDouble(), 0, 1) * (maximum - minimum);

    private void TryAutonomousJump(PetInstance pet, string? supportingSurfaceId)
    {
        if (pet.JumpAttemptDelay > 0 || pet.State != PetState.Walk)
            return;
        var desire = pet.BehaviorOverrides.ResolveJumpDesire(JumpDesire);
        var windowDesire = pet.BehaviorOverrides.ResolveWindowJumpDesire(WindowJumpDesire);
        pet.JumpAttemptDelay = RandomDelay(InitialJumpDelayMin, InitialJumpDelayMax);

        var speed = Math.Clamp(pet.Definition.Movement.WalkSpeed, 0, 1000);
        var baseJumpSpeed = Math.Max(0, pet.Definition.Movement.JumpSpeed);
        var maximumJumpSpeed = baseJumpSpeed * WindowJumpSpeedMultiplier;
        var scale = EffectiveScale(pet);
        var leftMargin = pet.Definition.Anchor.X * scale + 4f;
        var rightMargin = MathF.Round(pet.Definition.CellWidth * scale) - pet.Definition.Anchor.X * scale + 4f;
        bool Plan(FacingDirection facing, out JumpTarget target)
            => JumpTargetPlanner.TrySelect(Surface.JumpCandidates, pet.FootPosition, facing, speed,
                baseJumpSpeed, pet.Definition.Movement.Gravity, pet.Definition.Movement.MaxFallSpeed,
                leftMargin, out target, Surface.WorkingAreaForPoint(pet.FootPosition), rightMargin,
                maximumJumpSpeed, supportingSurfaceId);
        if (Surface.WindowLandingEnabled && windowDesire > 0 &&
            _random.NextDouble() < windowDesire &&
            (Plan(pet.Facing, out var target) || Plan(pet.Facing == FacingDirection.Right ? FacingDirection.Left : FacingDirection.Right, out target)))
        {
            BeginPlannedJumpCharge(pet, target.LaunchSpeed);
            pet.Velocity = new Vector2(target.HorizontalVelocity, 0);
            if (Math.Abs(target.HorizontalVelocity) > 0.001f)
                pet.Facing = target.HorizontalVelocity < 0 ? FacingDirection.Left : FacingDirection.Right;
            return;
        }

        if (desire <= 0 || _random.NextDouble() >= desire)
            return;
        if (pet.Definition.Movement.JumpSpeed <= 0)
            return;

        if (_random.NextDouble() >= 0.5)
        {
            var duration = RandomDelay(JumpChargeDurationMin, JumpChargeDurationMax);
            BeginJumpCharge(pet, duration);
        }
        else
            pet.StartJump();
    }

    private bool TryStartClimb(PetInstance pet, WorldRect area, float delta)
    {
        if (pet.ClimbAttemptDelay > 0 || pet.State != PetState.Walk || Surface.DisplayProvider is null)
            return false;
        var desire = pet.BehaviorOverrides.ResolveClimbDesire(ClimbDesire);
        var display = Surface.DisplayForPoint(pet.FootPosition);
        if (display is null || !display.Value.WorkingArea.Equals(area)) return false;

        var scale = EffectiveScale(pet);
        var width = MathF.Round(pet.Definition.CellWidth * scale);
        var height = MathF.Round(pet.Definition.CellHeight * scale);
        var anchorX = pet.Definition.Anchor.X * scale;
        var anchorY = pet.Definition.Anchor.Y * scale;
        var safeLeft = area.Left + anchorX + 4f;
        var safeRight = area.Right - (width - anchorX) - 4f;
        var safeTop = area.Top + anchorY + 4f;
        if (safeRight <= safeLeft || height + 8 >= area.Height || pet.Definition.Movement.WalkSpeed <= 0)
            return false;

        var nextX = pet.FootPosition.X + (int)pet.Facing * Math.Clamp(pet.Definition.Movement.WalkSpeed, 0, 1000) * delta;
        var side = pet.Facing == FacingDirection.Right && nextX >= safeRight ? FacingDirection.Right
            : pet.Facing == FacingDirection.Left && nextX <= safeLeft ? FacingDirection.Left : (FacingDirection?)null;
        if (side is null) return false;
        if (!pet.ClimbEdgeArmed && pet.ClimbSide == side
            && string.Equals(pet.LastClimbAttemptDisplayId, display.Value.Id.Value, StringComparison.OrdinalIgnoreCase)) return false;

        pet.ClimbAttemptDelay = RandomDelay(InitialClimbDelayMin, InitialClimbDelayMax);
        pet.ClimbEdgeArmed = false;
        pet.LastClimbAttemptDisplayId = display.Value.Id.Value;
        pet.ClimbSide = side.Value;
        pet.ClimbEdgeX = side == FacingDirection.Right ? safeRight : safeLeft;
        if (desire <= 0 || _random.NextDouble() >= desire)
            return false;

        pet.ClimbTopY = safeTop;
        pet.AttachedDisplayId = display.Value.Id.Value;
        pet.AttachedDisplayArea = area;
        pet.AttachedDisplayScale = display.Value.Scale;
        pet.AttachedEffectiveScale = scale;
        pet.FootPosition = new Vector2(pet.ClimbEdgeX, area.Bottom);
        pet.Facing = side.Value;
        pet.Velocity = Vector2.Zero;
        pet.State = PetState.Climb;
        pet.StateTime = 0;
        return true;
    }

    private void IntegrateClimbing(PetInstance pet, float delta)
    {
        var speed = Math.Clamp(pet.Definition.Movement.WalkSpeed, 0, 1000);
        var y = pet.FootPosition.Y - speed * delta;
        if (y <= pet.ClimbTopY)
        {
            pet.FootPosition = new Vector2(pet.ClimbEdgeX, pet.ClimbTopY);
            pet.Facing = pet.ClimbSide == FacingDirection.Right ? FacingDirection.Left : FacingDirection.Right;
            pet.TopDropDelay = RandomDelay(TopDropDelayMin, TopDropDelayMax);
            pet.State = PetState.TopWalk;
            pet.StateTime = 0;
            pet.Velocity = Vector2.Zero;
            return;
        }

        pet.FootPosition = new Vector2(pet.ClimbEdgeX, y);
        pet.Velocity = Vector2.Zero;
    }

    private void IntegrateTopWalking(PetInstance pet, float delta)
    {
        var scale = pet.AttachedEffectiveScale;
        var width = MathF.Round(pet.Definition.CellWidth * scale);
        var anchorX = pet.Definition.Anchor.X * scale;
        var left = pet.AttachedDisplayArea.Left + anchorX + 4f;
        var right = pet.AttachedDisplayArea.Right - (width - anchorX) - 4f;
        var speed = Math.Clamp(pet.Definition.Movement.WalkSpeed, 0, 1000);
        var x = pet.FootPosition.X + (int)pet.Facing * speed * delta;
        if (pet.StateTime >= pet.TopDropDelay
            || (pet.Facing == FacingDirection.Left && x <= left)
            || (pet.Facing == FacingDirection.Right && x >= right))
        {
            pet.FootPosition = new Vector2(Math.Clamp(x, left, right), pet.ClimbTopY);
            TransitionToFall(pet);
            return;
        }

        pet.FootPosition = new Vector2(x, pet.ClimbTopY);
        pet.Velocity = new Vector2((int)pet.Facing * speed, 0);
    }

    private bool ValidateAttachment(PetInstance pet)
    {
        var display = Surface.DisplayForPoint(pet.FootPosition);
        if (display is null || !string.Equals(display.Value.Id.Value, pet.AttachedDisplayId, StringComparison.OrdinalIgnoreCase))
            return false;
        return display.Value.WorkingArea.Equals(pet.AttachedDisplayArea)
            && Math.Abs(display.Value.Scale - pet.AttachedDisplayScale) < 0.001f
            && Math.Abs(EffectiveScale(pet) - pet.AttachedEffectiveScale) < 0.001f;
    }

    private float EffectiveScale(PetInstance pet)
        => Math.Clamp(pet.Definition.Scale * pet.ResolveScale(GlobalScale) * Surface.DisplayScaleForPoint(pet.FootPosition), 0.1f, 64f);

    private void RearmClimbEdge(PetInstance pet)
    {
        if (pet.ClimbEdgeArmed) return;
        var display = Surface.DisplayForPoint(pet.FootPosition);
        if (display?.Id.Value != pet.LastClimbAttemptDisplayId
            || (pet.ClimbSide == FacingDirection.Right
                ? pet.FootPosition.X <= pet.ClimbEdgeX - ClimbEdgeRearmDistance
                : pet.FootPosition.X >= pet.ClimbEdgeX + ClimbEdgeRearmDistance))
            pet.ClimbEdgeArmed = true;
    }

    private static void TransitionToFall(PetInstance pet)
    {
        pet.State = PetState.Fall;
        pet.StateTime = 0;
        pet.Velocity = Vector2.Zero;
        pet.ClearJumpCharge();
        pet.AttachedDisplayId = null;
    }

    private static float WrapHorizontal(float x, WorldRect travelBounds)
    {
        var width = travelBounds.Right - travelBounds.Left;
        if (width <= 0) return x;

        while (x < travelBounds.Left) x += width;
        while (x > travelBounds.Right) x -= width;
        return x;
    }

    private static void RelocateToSurface(PetInstance pet, WorldRect area)
    {
        pet.FootPosition = area.ClampFoot(pet.FootPosition);
        pet.Velocity = Vector2.Zero;
        pet.ClearJumpCharge();
        pet.State = PetState.Fall;
        pet.StateTime = 0;
    }

    private static Vector2 GetNextFootPosition(PetInstance pet, float delta, WorldRect travelBounds)
    {
        if (pet.State == PetState.Walk)
        {
            var speed = Math.Clamp(pet.Definition.Movement.WalkSpeed, 0, 1000);
            return pet.FootPosition + new Vector2((int)pet.Facing * speed * delta, 0);
        }

        if (pet.State is PetState.Jump or PetState.Apex or PetState.Fall)
        {
            var (displacement, _) = BallisticMotion.Advance(pet.Velocity.Y,
                pet.Definition.Movement.Gravity, pet.Definition.Movement.MaxFallSpeed, delta);
            return new Vector2(
                WrapHorizontal(pet.FootPosition.X + pet.Velocity.X * delta, travelBounds),
                pet.FootPosition.Y + displacement);
        }

        if (pet.State == PetState.JumpCharge)
            return pet.FootPosition;

        if (pet.State == PetState.Climb)
            return new Vector2(pet.ClimbEdgeX, pet.FootPosition.Y - Math.Clamp(pet.Definition.Movement.WalkSpeed, 0, 1000) * delta);

        if (pet.State == PetState.TopWalk)
            return new Vector2(pet.FootPosition.X + (int)pet.Facing * Math.Clamp(pet.Definition.Movement.WalkSpeed, 0, 1000) * delta, pet.FootPosition.Y);

        return pet.FootPosition;
    }
}
