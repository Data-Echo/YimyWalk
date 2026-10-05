using YimyWalk.Core.Characters;

namespace YimyWalk.Core.Startup;

/// <summary>
/// A user-facing startup problem with a stable code and a concrete recovery
/// action. The record is platform-neutral so the App and command-line checks
/// can present the same diagnosis without pulling desktop APIs into Core.
/// </summary>
public sealed record StartupDiagnostic(
    string Code,
    string Summary,
    string Details,
    string Recovery)
{
    public override string ToString()
        => $"[{Code}] {Summary} {Details} Recovery: {Recovery}";
}

public static class StartupDiagnostics
{
    /// <summary>
    /// Validates the character input needed before the host starts its timer.
    /// A partially broken directory is allowed when at least one role remains
    /// usable; the manager reports those individual failures after startup.
    /// </summary>
    public static StartupDiagnostic? ValidateCharacters(
        string root,
        IReadOnlyDictionary<string, CharacterDefinition> definitions,
        IReadOnlyList<CharacterScanFailure> failures,
        bool? rootExists = null)
    {
        ArgumentNullException.ThrowIfNull(definitions);
        ArgumentNullException.ThrowIfNull(failures);

        var exists = rootExists ?? (!string.IsNullOrWhiteSpace(root) && Directory.Exists(root));
        if (!exists)
        {
            return new StartupDiagnostic(
                "CHARACTER_ROOT_MISSING",
                "未找到 Characters 角色目录。",
                string.IsNullOrWhiteSpace(root) ? "Characters 路径为空。" : $"角色目录：{root}",
                "重新运行 publish 发布脚本，或在发布目录旁创建 Characters/<角色ID>/sprite.png。");
        }

        if (definitions.Count > 0)
            return null;

        if (failures.Count == 0)
        {
            return new StartupDiagnostic(
                "CHARACTERS_EMPTY",
                "角色目录为空，没有可启动的角色。",
                $"角色目录：{root}",
                "至少放入一个包含有效 3×3 sprite.png 的角色目录，然后重新启动。");
        }

        var details = string.Join("；", failures.Select(f =>
            $"{Path.GetFileName(f.DirectoryPath)}：{f.Reason}"));
        return new StartupDiagnostic(
            "CHARACTERS_INVALID",
            "角色目录中的角色均无法加载。",
            details,
            "修复上面列出的角色，或从发布包恢复 Characters 目录后重新启动。");
    }
}
