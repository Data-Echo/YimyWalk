using System.Numerics;

namespace YimyWalk.Core.Simulation;

public sealed class PetEngine
{
    public const float MaxTossSpeed = 1200f;
    public const double MaxDeltaSeconds = 0.1;

    private readonly IRandomSource _random;
    public bool HungerEnabled { get; set; } = true;
    public float NeedRate { get; set; } = 1f;

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
        var delta = (float)Math.Clamp(deltaSeconds, 0, MaxDeltaSeconds);
        pet.AdvanceNeeds(delta, HungerEnabled, NeedRate);
        if (!Surface.IsValid)
        {
            RelocateToSurface(pet, Surface.WorkingAreaForPoint(pet.FootPosition));
            return;
        }

        var travelBounds = Surface.HorizontalTravelBounds;
        var nextFootPosition = GetNextFootPosition(pet, delta, travelBounds);
        var halfWidth = pet.State is PetState.Jump or PetState.Apex or PetState.Fall
            ? Math.Max(1f, pet.Definition.CellWidth * pet.Definition.Scale / 2f)
            : 0f;
        var selectedSurface = Surface.SurfaceForPet(pet.FootPosition, halfWidth, nextFootPosition);
        var area = selectedSurface.Area;
        if ((pet.State is PetState.Idle or PetState.Walk)
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
        }
        pet.StateTime += delta;
        pet.AnimationTime += delta;

        if (pet.State == PetState.Dragged)
        {
            pet.Velocity = Vector2.Zero;
            return;
        }

        switch (pet.State)
        {
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
                IntegrateWalking(pet, area, Surface.HorizontalTravelBounds, delta);
                break;
            case PetState.Idle:
                pet.Velocity = Vector2.Zero;
                if (pet.StateTime >= pet.Definition.Defaults.IdleSecondsMax
                    || (pet.StateTime >= pet.Definition.Defaults.IdleSecondsMin && _random.NextDouble() >= 0.25))
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
        }
    }

    public void BeginDrag(PetInstance pet)
    {
        ArgumentNullException.ThrowIfNull(pet);
        pet.State = PetState.Dragged;
        pet.StateTime = 0;
        pet.Velocity = Vector2.Zero;
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

    private static void IntegrateAirborne(PetInstance pet, WorldRect area, WorldRect travelBounds, float delta)
    {
        var movement = pet.Definition.Movement;
        var vy = Math.Min(movement.MaxFallSpeed, pet.Velocity.Y + movement.Gravity * delta);
        var next = new Vector2(
            WrapHorizontal(pet.FootPosition.X + pet.Velocity.X * delta, travelBounds),
            pet.FootPosition.Y + vy * delta);
        if (next.Y >= area.Bottom)
        {
            pet.FootPosition = new Vector2(
                travelBounds.Width > 0 ? next.X : Math.Clamp(next.X, area.Left, area.Right),
                area.Bottom);
            pet.Velocity = Vector2.Zero;
            pet.State = PetState.Idle;
            pet.StateTime = 0;
            return;
        }

        pet.Velocity = new Vector2(pet.Velocity.X, vy);
        pet.FootPosition = new Vector2(
            travelBounds.Width > 0 ? next.X : Math.Clamp(next.X, area.Left, area.Right),
            next.Y);
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
            var vy = Math.Min(pet.Definition.Movement.MaxFallSpeed,
                pet.Velocity.Y + pet.Definition.Movement.Gravity * delta);
            return new Vector2(
                WrapHorizontal(pet.FootPosition.X + pet.Velocity.X * delta, travelBounds),
                pet.FootPosition.Y + vy * delta);
        }

        return pet.FootPosition;
    }
}
