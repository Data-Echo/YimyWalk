using System.Numerics;

namespace YimyWalk.Core.Simulation;

public enum VirtualFoodState
{
    Available,
    Claimed,
    Consumed
}

public sealed class VirtualFoodTarget
{
    internal VirtualFoodTarget(Vector2 position) => Position = position;

    public Guid Id { get; } = Guid.NewGuid();
    public Vector2 Position { get; internal set; }
    public VirtualFoodState State { get; internal set; }
    public Guid? ClaimantId { get; internal set; }
    public float VisibleScale { get; internal set; } = 1f;
    internal double AgeSeconds { get; set; }
    internal double ConsumeSeconds { get; set; }
}

/// <summary>
/// Owns program-generated food targets only. It never reads or mutates desktop
/// files, shortcuts, or windows.
/// </summary>
public sealed class VirtualFoodSystem
{
    private const float TravelSpeed = 180f;
    private const double TargetLifetimeSeconds = 8;
    private const double ConsumeLifetimeSeconds = 0.35;
    private readonly List<VirtualFoodTarget> _targets = new();

    public VirtualFoodSystem(int maxTargets = 32)
        => MaxTargets = Math.Clamp(maxTargets, 1, 256);

    public int MaxTargets { get; }
    public IReadOnlyList<VirtualFoodTarget> Targets => _targets;

    public VirtualFoodTarget Spawn(Vector2 position)
    {
        if (!float.IsFinite(position.X) || !float.IsFinite(position.Y))
            throw new ArgumentOutOfRangeException(nameof(position));
        if (_targets.Count >= MaxTargets)
            throw new InvalidOperationException("virtual food target limit reached");
        var target = new VirtualFoodTarget(position);
        _targets.Add(target);
        return target;
    }

    public bool TryClaim(PetInstance pet, Guid targetId)
    {
        ArgumentNullException.ThrowIfNull(pet);
        if (pet.State is PetState.Dragged or PetState.Jump or PetState.Apex or PetState.Fall or PetState.Eat)
            return false;
        var target = _targets.FirstOrDefault(item => item.Id == targetId);
        if (target is null || target.State != VirtualFoodState.Available) return false;
        if (_targets.Any(item => item.State == VirtualFoodState.Claimed && item.ClaimantId == pet.InstanceId)) return false;
        target.State = VirtualFoodState.Claimed;
        target.ClaimantId = pet.InstanceId;
        return true;
    }

    public void Tick(IEnumerable<PetInstance> pets, double deltaSeconds)
    {
        ArgumentNullException.ThrowIfNull(pets);
        var delta = Math.Clamp(deltaSeconds, 0, 0.1);
        var byId = pets.ToDictionary(pet => pet.InstanceId);
        foreach (var target in _targets.ToArray())
        {
            target.AgeSeconds += delta;
            if (target.State == VirtualFoodState.Available && target.AgeSeconds >= TargetLifetimeSeconds)
            {
                _targets.Remove(target);
                continue;
            }
            if (target.State == VirtualFoodState.Consumed)
            {
                target.ConsumeSeconds += delta;
                target.VisibleScale = Math.Max(0, 1f - (float)(target.ConsumeSeconds / ConsumeLifetimeSeconds));
                if (target.ConsumeSeconds >= ConsumeLifetimeSeconds) _targets.Remove(target);
                continue;
            }
            if (target.State != VirtualFoodState.Claimed || target.ClaimantId is not { } claimantId
                || !byId.TryGetValue(claimantId, out var pet)
                || pet.State is PetState.Dragged or PetState.Jump or PetState.Apex or PetState.Fall or PetState.Eat)
            {
                if (target.State == VirtualFoodState.Claimed)
                {
                    target.State = VirtualFoodState.Available;
                    target.ClaimantId = null;
                }
                continue;
            }

            var offset = target.Position - pet.FootPosition;
            var distance = offset.Length();
            if (distance <= TravelSpeed * delta || distance <= 2f)
            {
                pet.FootPosition = target.Position;
                pet.Feed(0.5f);
                target.State = VirtualFoodState.Consumed;
                target.ClaimantId = pet.InstanceId;
                target.VisibleScale = 1f;
                continue;
            }

            var step = offset / distance * TravelSpeed * (float)delta;
            pet.FootPosition += step;
            pet.Facing = step.X < 0 ? FacingDirection.Left : FacingDirection.Right;
            pet.State = PetState.Walk;
        }
    }

    public void Clear() => _targets.Clear();
}
