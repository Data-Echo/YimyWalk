using YimyWalk.Core.Characters;

namespace YimyWalk.Core.Tests;

public sealed class LoaderValidationTests
{
    [Theory]
    [InlineData("{\"scale\":0.05}")]
    [InlineData("{\"scale\":9}")]
    [InlineData("{\"anchor\":true}")]
    [InlineData("{\"movement\":[]}")]
    [InlineData("{\"animation\":\"fast\"}")]
    [InlineData("{\"animation\":{\"idleFps\":0,\"walkFps\":8}}")]
    [InlineData("{\"movement\":{\"walkSpeed\":2001}}")]
    public void InvalidOptionalConfigurationIncludesFullCharacterDirectory(string json)
    {
        var directory = TestSpriteFactory.WriteSprite(96, 96, fullyTransparent: false);
        try
        {
            File.WriteAllText(Path.Combine(directory, "character.json"), json);

            var exception = Assert.Throws<CharacterLoadException>(() => CharacterLoader.LoadDirectory(directory));

            Assert.Equal(Path.GetFullPath(directory), exception.DirectoryPath);
            Assert.Contains(Path.GetFullPath(directory), exception.Message, StringComparison.Ordinal);
        }
        finally
        {
            TestSpriteFactory.DeleteFixture(directory);
        }
    }

    [Fact]
    public void RejectsScaleThatWouldRenderAnOversizedFrame()
    {
        var directory = TestSpriteFactory.WriteSprite(1536, 1536, fullyTransparent: false);
        try
        {
            File.WriteAllText(Path.Combine(directory, "character.json"), "{\"scale\":3}");

            var exception = Assert.Throws<CharacterLoadException>(() => CharacterLoader.LoadDirectory(directory));

            Assert.Equal(Path.GetFullPath(directory), exception.DirectoryPath);
        }
        finally
        {
            TestSpriteFactory.DeleteFixture(directory);
        }
    }

    [Fact]
    public void ReadsGroupedDialogueAndFallsBackByGroup()
    {
        var directory = TestSpriteFactory.WriteSprite(96, 96, fullyTransparent: false);
        try
        {
            File.WriteAllText(Path.Combine(directory, "dialogue.txt"), """
            [walk]
            走走。

            [idle]
            发呆。
            """);

            var dialogue = CharacterLoader.LoadDirectory(directory).Dialogue;

            Assert.Equal(new[] { "走走。" }, dialogue.Get("walk"));
            Assert.Equal(new[] { "呀！" }, dialogue.Get("jump"));
        }
        finally
        {
            TestSpriteFactory.DeleteFixture(directory);
        }
    }

    [Fact]
    public void CharacterFramesAndDialogueOutputsAreImmutableSnapshots()
    {
        var directory = TestSpriteFactory.WriteSprite(96, 96, fullyTransparent: false);
        try
        {
            var definition = CharacterLoader.LoadDirectory(directory);

            Assert.IsNotType<List<CharacterFrame>>(definition.Frames);
            Assert.IsNotType<List<string>>(definition.Dialogue.Get("idle"));
        }
        finally
        {
            TestSpriteFactory.DeleteFixture(directory);
        }
    }
}
