using System.Numerics;
using YimyWalk.Core.Characters;
using YimyWalk.Core.Simulation;

namespace YimyWalk.Core.Tests;

public sealed class PetNeedsTests
{
    [Fact]
    public void SpriteOnlyDefaultsExposeSafeNeedValues()
    {
        var definition = TestSpriteFactory.LoadSpriteOnly("needs");
        Assert.Equal(0.01f, definition.Defaults.HungerIncreasePerMinute);
        var pet = new PetInstance(definition, new Vector2(10, 720));
        Assert.Equal(0f, pet.Hunger);
        Assert.Equal(0f, pet.Boredom);
    }

    [Fact]
    public void HungerAndBoredomAdvancePerInstanceAndFeedingReducesHunger()
    {
        var first = new PetInstance(TestSpriteFactory.LoadSpriteOnly("first"), new Vector2(10, 720));
        var second = new PetInstance(first.Definition, new Vector2(20, 720));
        var engine = new PetEngine(new TestSurface(), new FixedRandom());

        for (var i = 0; i < 600; i++) engine.Tick(first, 0.1);

        Assert.InRange(first.Hunger, 0.009f, 0.011f);
        Assert.InRange(first.Boredom, 0.009f, 0.101f);
        Assert.Equal(0f, second.Hunger);
        Assert.Equal(0f, second.Boredom);
        Assert.True(first.Feed());
        Assert.InRange(first.Hunger, 0f, 0.01f);
        Assert.Equal(PetState.Eat, first.State);
        Assert.False(first.Feed());
    }

    [Fact]
    public void NeedRateAndHungerToggleAreBounded()
    {
        var pet = new PetInstance(TestSpriteFactory.LoadSpriteOnly("needs"), new Vector2(10, 720));
        var engine = new PetEngine(new TestSurface(), new FixedRandom()) { HungerEnabled = false, NeedRate = 4f };
        for (var i = 0; i < 600; i++) engine.Tick(pet, 0.1);
        Assert.Equal(0f, pet.Hunger);
        engine.HungerEnabled = true;
        engine.NeedRate = 99f;
        for (var i = 0; i < 600; i++) engine.Tick(pet, 0.1);
        Assert.Equal(0.04f, pet.Hunger, 3);
    }

    [Fact]
    public void ConfiguredHungerRateIsLoaded()
    {
        var directory = TestSpriteFactory.WriteSprite(96, 96, false, "configured-needs");
        try
        {
            File.WriteAllText(Path.Combine(directory, "character.json"), "{\"schemaVersion\":1,\"behavior\":{\"hungerIncreasePerMinute\":0.25}}");
            var definition = CharacterLoader.LoadDirectory(directory);
            Assert.Equal(0.25f, definition.Defaults.HungerIncreasePerMinute);
        }
        finally { TestSpriteFactory.DeleteFixture(directory); }
    }

    private sealed class TestSurface : IWorldSurface
    {
        public WorldRect WorkingArea => new(0, 0, 1280, 720);
        public bool IsValid => true;
    }

    private sealed class FixedRandom : IRandomSource
    {
        public double NextDouble() => 0.5;
        public int Next(int minInclusive, int maxExclusive) => minInclusive;
    }
}
