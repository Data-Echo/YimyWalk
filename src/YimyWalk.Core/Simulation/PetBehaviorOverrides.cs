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
    BooleanOverride DialogueOverride = BooleanOverride.Inherit)
{
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

    private static float ClampRate(float value)
        => float.IsFinite(value) ? Math.Clamp(value, 0.1f, 4f) : 1f;
}
