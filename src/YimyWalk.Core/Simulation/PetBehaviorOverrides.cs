namespace YimyWalk.Core.Simulation;

public enum BooleanOverride
{
    Inherit,
    Enabled,
    Disabled
}

public static class BooleanOverrideExtensions
{
    public static bool Resolve(this BooleanOverride value, bool globalValue)
        => value switch
        {
            BooleanOverride.Enabled => true,
            BooleanOverride.Disabled => false,
            _ => globalValue
        };
}

public sealed record PetBehaviorOverrides(
    BooleanOverride WalkOverride = BooleanOverride.Inherit,
    float? ActivityRateOverride = null,
    BooleanOverride DialogueOverride = BooleanOverride.Inherit,
    float? JumpDesireOverride = null,
    float? ClimbDesireOverride = null,
    float? WindowJumpDesireOverride = null)
{
    private float? _jumpDesireOverride = NormalizeDesire(JumpDesireOverride);
    private float? _climbDesireOverride = NormalizeDesire(ClimbDesireOverride);
    private float? _windowJumpDesireOverride = NormalizeDesire(WindowJumpDesireOverride);
    public float? JumpDesireOverride { get => _jumpDesireOverride; init => _jumpDesireOverride = NormalizeDesire(value); }
    public float? ClimbDesireOverride { get => _climbDesireOverride; init => _climbDesireOverride = NormalizeDesire(value); }
    public float? WindowJumpDesireOverride { get => _windowJumpDesireOverride; init => _windowJumpDesireOverride = NormalizeDesire(value); }

    public static PetBehaviorOverrides Inherit => new();

    public bool ResolveWalkEnabled(bool globalValue)
        => WalkOverride.Resolve(globalValue);

    public float ResolveActivityRate(float globalValue)
    {
        var global = ClampRate(globalValue);
        return ActivityRateOverride is { } custom ? ClampRate(custom) : global;
    }

    public bool ResolveDialogueEnabled(bool globalValue)
        => DialogueOverride.Resolve(globalValue);

    public float ResolveJumpDesire(float globalValue)
        => ResolveDesire(globalValue, JumpDesireOverride);

    public float ResolveClimbDesire(float globalValue)
        => ResolveDesire(globalValue, ClimbDesireOverride);

    public float ResolveWindowJumpDesire(float globalValue)
        => ResolveDesire(globalValue, WindowJumpDesireOverride);

    private static float ClampRate(float value)
        => float.IsFinite(value) ? Math.Clamp(value, 0.1f, 4f) : 1f;

    private static float ResolveDesire(float globalValue, float? custom)
        => ClampDesire(custom ?? globalValue);

    private static float ClampDesire(float value)
        => float.IsFinite(value) ? Math.Clamp(value, 0f, 1f) : 0f;

    private static float? NormalizeDesire(float? value)
        => value is { } number && float.IsFinite(number) ? Math.Clamp(number, 0f, 1f) : null;
}
