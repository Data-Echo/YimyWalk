using YimyWalk.Core.Characters;

namespace YimyWalk.Core.Tests;

public sealed class CharacterFormatTests
{
    [Fact]
    public void SelectsTheApprovedNineFrameProtocol()
    {
        var definition = TestSpriteFactory.LoadSpriteOnly("cat");

        Assert.Equal(9, definition.Frames.Count);
        Assert.Equal(FrameKind.Idle, definition.Frames[0].Kind);
        Assert.Equal(FrameKind.Idle, definition.Frames[2].Kind);
        Assert.Equal(FrameKind.WalkRight, definition.Frames[3].Kind);
        Assert.Equal(FrameKind.WalkRight, definition.Frames[5].Kind);
        Assert.Equal(FrameKind.Jump, definition.Frames[6].Kind);
        Assert.Equal(FrameKind.Apex, definition.Frames[7].Kind);
        Assert.Equal(FrameKind.Fall, definition.Frames[8].Kind);
        Assert.Equal(new Anchor(16, 28.8f), definition.Anchor);
        Assert.True(definition.SelectFrame(FrameKind.WalkLeft).Mirrored);
    }

    [Fact]
    public void RejectsNonDivisibleDimensionsAndFullyTransparentImages()
    {
        var nonDivisible = TestSpriteFactory.WriteSprite(97, 96, fullyTransparent: false);
        var transparent = TestSpriteFactory.WriteSprite(96, 96, fullyTransparent: true);
        try
        {
            Assert.Throws<CharacterLoadException>(() => CharacterLoader.LoadDirectory(nonDivisible));
            Assert.Throws<CharacterLoadException>(() => CharacterLoader.LoadDirectory(transparent));
        }
        finally
        {
            TestSpriteFactory.DeleteFixture(nonDivisible);
            TestSpriteFactory.DeleteFixture(transparent);
        }
    }

    [Fact]
    public void RejectsCorruptAndOverlargeInputs()
    {
        var corrupt = TestSpriteFactory.WriteCorruptSprite();
        var overlarge = TestSpriteFactory.WriteSprite(2049, 2049, fullyTransparent: false);
        try
        {
            Assert.Throws<CharacterLoadException>(() => CharacterLoader.LoadDirectory(corrupt));
            Assert.Throws<CharacterLoadException>(() => CharacterLoader.LoadDirectory(overlarge));
        }
        finally
        {
            TestSpriteFactory.DeleteFixture(corrupt);
            TestSpriteFactory.DeleteFixture(overlarge);
        }
    }

    [Fact]
    public void RejectsOpaqueRgbPngWithoutAnAlphaChannel()
    {
        var directory = TestSpriteFactory.WriteOpaqueRgbSprite();

        try
        {
            var exception = Assert.Throws<CharacterLoadException>(() => CharacterLoader.LoadDirectory(directory));
            Assert.Equal(Path.GetFullPath(directory), exception.DirectoryPath);
        }
        finally
        {
            TestSpriteFactory.DeleteFixture(directory);
        }
    }
}
