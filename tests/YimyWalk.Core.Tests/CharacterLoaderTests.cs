using YimyWalk.Core.Characters;

namespace YimyWalk.Core.Tests;

public sealed class CharacterLoaderTests
{
    [Fact]
    public void SpriteOnlyDirectoryUsesSafeDefaults()
    {
        var definition = TestSpriteFactory.LoadSpriteOnly("slime");

        Assert.Equal("slime", definition.Id);
        Assert.Equal("slime", definition.DisplayName);
        Assert.Equal(32, definition.CellWidth);
        Assert.Equal(32, definition.CellHeight);
        Assert.Equal(1f, definition.Scale);
        Assert.Equal(60f, definition.Movement.WalkSpeed);
        Assert.NotEmpty(definition.Dialogue.Get("idle"));
    }

    [Fact]
    public void ReadsOptionalConfigWithoutChangingFrameProtocol()
    {
        var directory = TestSpriteFactory.WriteSprite(96, 96, fullyTransparent: false);
        File.WriteAllText(Path.Combine(directory, "character.json"), """
        {
          "schemaVersion": 1,
          "displayName": "配置小猫",
          "scale": 1.5,
          "anchor": { "x": 0.5, "y": 0.9 },
          "movement": { "walkSpeed": 80, "jumpSpeed": 450, "gravity": 1500, "maxFallSpeed": 1500 }
        }
        """);

        var definition = CharacterLoader.LoadDirectory(directory);

        Assert.Equal("配置小猫", definition.DisplayName);
        Assert.Equal(1.5f, definition.Scale);
        Assert.Equal(80f, definition.Movement.WalkSpeed);
        Assert.Equal(9, definition.Frames.Count);
    }
}
