using System.Numerics;
using YimyWalk.Core.Characters;
using YimyWalk.Core.Simulation;

namespace YimyWalk.Core.Tests;

public sealed class DialogueSchedulerTests
{
    [Fact]
    public void MissingGroupFallsBackAndPerPetCooldownIsEnforced()
    {
        var pet = new PetInstance(TestSpriteFactory.LoadSpriteOnly("talker"), new Vector2(10, 720));
        var scheduler = new DialogueScheduler(maxVisible: 3);

        var first = scheduler.TryPublish(pet, "clicked", 0);
        var blocked = scheduler.TryPublish(pet, "clicked", 1);
        var next = scheduler.TryPublish(pet, "clicked", 46);

        Assert.True(first.Accepted);
        Assert.Equal("……", first.Bubble!.Text);
        Assert.False(blocked.Accepted);
        Assert.True(next.Accepted);
    }

    [Fact]
    public void ActiveBubbleLimitAndExpiryAreEnforced()
    {
        var scheduler = new DialogueScheduler(maxVisible: 2, lifetimeSeconds: 2);
        var pets = Enumerable.Range(0, 3)
            .Select(index => new PetInstance(TestSpriteFactory.LoadSpriteOnly($"talker-{index}"), new Vector2(index, 720)))
            .ToArray();

        Assert.True(scheduler.TryPublish(pets[0], "idle", 0).Accepted);
        Assert.True(scheduler.TryPublish(pets[1], "idle", 0).Accepted);
        Assert.False(scheduler.TryPublish(pets[2], "idle", 0).Accepted);
        Assert.Equal(2, scheduler.Active.Count);
        scheduler.Advance(2.1);
        Assert.Empty(scheduler.Active);
        Assert.True(scheduler.TryPublish(pets[2], "idle", 2.1).Accepted);
    }

    [Fact]
    public void ClearRemovesAllBubblesAndCooldowns()
    {
        var pet = new PetInstance(TestSpriteFactory.LoadSpriteOnly("talker-clear"), new Vector2(10, 720));
        var scheduler = new DialogueScheduler();
        Assert.True(scheduler.TryPublish(pet, "idle", 0).Accepted);
        scheduler.Clear();
        Assert.Empty(scheduler.Active);
        Assert.True(scheduler.TryPublish(pet, "idle", 0.1).Accepted);
    }
}
