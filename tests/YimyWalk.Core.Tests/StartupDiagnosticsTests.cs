using YimyWalk.Core.Characters;
using YimyWalk.Core.Startup;

namespace YimyWalk.Core.Tests;

public sealed class StartupDiagnosticsTests
{
    [Fact]
    public void MissingCharacterRootProducesActionableDiagnostic()
    {
        var diagnostic = StartupDiagnostics.ValidateCharacters(
            @"C:\missing\Characters",
            new Dictionary<string, CharacterDefinition>(),
            Array.Empty<CharacterScanFailure>(),
            rootExists: false);

        Assert.NotNull(diagnostic);
        Assert.Equal("CHARACTER_ROOT_MISSING", diagnostic.Code);
        Assert.Contains("Characters", diagnostic.Summary, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("publish", diagnostic.Recovery, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EmptyRootProducesDistinctDiagnostic()
    {
        var diagnostic = StartupDiagnostics.ValidateCharacters(
            @"C:\package\Characters",
            new Dictionary<string, CharacterDefinition>(),
            Array.Empty<CharacterScanFailure>(),
            rootExists: true);

        Assert.NotNull(diagnostic);
        Assert.Equal("CHARACTERS_EMPTY", diagnostic.Code);
    }

    [Fact]
    public void AllInvalidRolesIncludeFailureReasons()
    {
        var failures = new[]
        {
            new CharacterScanFailure(@"C:\package\Characters\cat", "sprite.png is missing"),
            new CharacterScanFailure(@"C:\package\Characters\bad", "sprite is fully transparent")
        };

        var diagnostic = StartupDiagnostics.ValidateCharacters(
            @"C:\package\Characters",
            new Dictionary<string, CharacterDefinition>(),
            failures,
            rootExists: true);

        Assert.NotNull(diagnostic);
        Assert.Equal("CHARACTERS_INVALID", diagnostic.Code);
        Assert.Contains("sprite.png is missing", diagnostic.Details, StringComparison.Ordinal);
        Assert.Contains("fully transparent", diagnostic.Details, StringComparison.Ordinal);
    }

    [Fact]
    public void ValidRoleAllowsStartupWhileInvalidRoleIsReportedAsWarning()
    {
        using var definition = TestSpriteFactory.LoadSpriteOnly("cat");
        var diagnostic = StartupDiagnostics.ValidateCharacters(
            @"C:\package\Characters",
            new Dictionary<string, CharacterDefinition>(StringComparer.OrdinalIgnoreCase)
            {
                [definition.Id] = definition
            },
            new[] { new CharacterScanFailure(@"C:\package\Characters\bad", "sprite.png is missing") },
            rootExists: true);

        Assert.Null(diagnostic);
    }
}
