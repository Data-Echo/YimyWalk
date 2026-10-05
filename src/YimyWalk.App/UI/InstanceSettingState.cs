using YimyWalk.Core.Simulation;

namespace YimyWalk.App.UI;

internal static class InstanceSettingState
{
    public static string WalkSource(PetBehaviorOverrides overrides, bool global, bool paused)
    {
        var source = overrides.WalkOverride switch
        {
            BooleanOverride.Enabled => "实例设置为开启",
            BooleanOverride.Disabled => "实例设置为关闭",
            _ => "跟随全局"
        };
        var effective = overrides.ResolveWalkEnabled(global);
        return paused && effective ? $"{source} · 当前受全局暂停限制" : $"{source} · 当前{(effective ? "开启" : "关闭")}";
    }

    public static string DialogueSource(PetBehaviorOverrides overrides, bool globalDefault, bool globalGate)
    {
        var source = overrides.DialogueOverride switch
        {
            BooleanOverride.Enabled => "实例设置为开启",
            BooleanOverride.Disabled => "实例设置为关闭",
            _ => "跟随全局"
        };
        var effective = overrides.ResolveDialogueEnabled(globalDefault);
        return !globalGate && effective
            ? $"{source} · 当前受全局关闭限制"
            : $"{source} · 当前{(effective ? "开启" : "关闭")}";
    }

    public static string ActivitySource(PetBehaviorOverrides overrides, float global)
    {
        var effective = overrides.ResolveActivityRate(global);
        return overrides.ActivityRateOverride is null
            ? $"跟随全局 · 当前活跃度 {effective:0.0}"
            : $"自定义 · 活跃度 {effective:0.0}";
    }
}
