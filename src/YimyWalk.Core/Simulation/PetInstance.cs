using System.Numerics;
using YimyWalk.Core.Characters;

namespace YimyWalk.Core.Simulation;

public sealed class PetInstance
{
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

    public void StartWalking(FacingDirection direction)
    {
        Facing = direction;
        State = PetState.Walk;
        StateTime = 0;
        Velocity = Vector2.Zero;
    }

    public void StartJump()
    {
        State = PetState.Jump;
        StateTime = 0;
        Velocity = new Vector2(Velocity.X, -Definition.Movement.JumpSpeed);
    }

    public FrameKind GetFrameKind()
        => State switch
        {
            PetState.Walk when Facing == FacingDirection.Left => FrameKind.WalkLeft,
            PetState.Walk => FrameKind.WalkRight,
            PetState.Jump => FrameKind.Jump,
            PetState.Apex => FrameKind.Apex,
            PetState.Fall => FrameKind.Fall,
            _ => FrameKind.Idle
        };
}
