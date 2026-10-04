using System.Numerics;
using YimyWalk.Core.Simulation;

namespace YimyWalk.Core.Tests;

public sealed class VirtualFoodTests
{
    [Fact]
    public void OnlyOnePetClaimsATargetAndConsumedFoodShrinksAway()
    {
        var definition = TestSpriteFactory.LoadSpriteOnly("food-pet");
        var first = new PetInstance(definition, new Vector2(0, 720));
        var second = new PetInstance(definition, new Vector2(10, 720));
        first.StartWalking(FacingDirection.Right);
        second.StartWalking(FacingDirection.Right);
        var foods = new VirtualFoodSystem(maxTargets: 4);
        var target = foods.Spawn(new Vector2(0, 720));

        Assert.True(foods.TryClaim(first, target.Id));
        Assert.False(foods.TryClaim(second, target.Id));
        foods.Tick(new[] { first, second }, 0.1);
        Assert.Equal(VirtualFoodState.Consumed, target.State);
        Assert.Equal(PetState.Eat, first.State);
        Assert.InRange(target.VisibleScale, 0f, 1f);
        for (var i = 0; i < 4; i++) foods.Tick(new[] { first, second }, 0.1);
        Assert.Empty(foods.Targets);
    }

    [Fact]
    public void InvalidOrInterruptedClaimIsReleasedWithoutStaleOwnership()
    {
        var pet = new PetInstance(TestSpriteFactory.LoadSpriteOnly("food-pet-2"), new Vector2(0, 720));
        var foods = new VirtualFoodSystem();
        var target = foods.Spawn(new Vector2(100, 720));
        pet.StartWalking(FacingDirection.Right);
        Assert.True(foods.TryClaim(pet, target.Id));
        pet.StartJump();
        foods.Tick(new[] { pet }, 0.1);
        Assert.Equal(VirtualFoodState.Available, target.State);
        Assert.Null(target.ClaimantId);
    }

    [Fact]
    public void TargetLimitAndMissingPetTimeoutAreBounded()
    {
        var foods = new VirtualFoodSystem(maxTargets: 1);
        Assert.NotNull(foods.Spawn(new Vector2(0, 0)));
        Assert.Throws<InvalidOperationException>(() => foods.Spawn(new Vector2(1, 1)));
        for (var i = 0; i < 100; i++) foods.Tick(Array.Empty<PetInstance>(), 0.1);
        Assert.Empty(foods.Targets);
    }
}
