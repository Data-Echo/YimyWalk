using System.Numerics;
using YimyWalk.Core.Characters;
using YimyWalk.Core.Display;
using YimyWalk.Core.Simulation;

namespace YimyWalk.Core.Tests;

public sealed class DisplayContractTests
{
    private static readonly DisplayInfo Left = new(
        new DisplayId("DISPLAY-LEFT"),
        new WorldRect(-1920, 0, 0, 1080),
        new WorldRect(-1920, 0, 0, 1040),
        1f,
        IsPrimary: false);

    private static readonly DisplayInfo Right = new(
        new DisplayId("DISPLAY-RIGHT"),
        new WorldRect(0, 0, 2560, 1440),
        new WorldRect(0, 0, 2560, 1400),
        1.25f,
        IsPrimary: true);

    [Fact]
    public void PointLookupPreservesNegativeDisplayCoordinates()
    {
        var displays = new DisplaySnapshot(new[] { Right, Left });

        var selected = displays.FindForPoint(new Vector2(-100, 500));

        Assert.True(selected.HasValue);
        Assert.Equal(Left.Id, selected.Value.Id);
        Assert.Equal(-1920, selected.Value.WorkingArea.Left);
    }

    [Fact]
    public void MissingPreferredDisplayFallsBackToPrimaryWithoutRewritingOrigin()
    {
        var displays = new DisplaySnapshot(new[] { Left, Right });

        var selected = displays.Resolve(new Vector2(10_000, 500), new DisplayId("MISSING"));

        Assert.True(selected.HasValue);
        Assert.Equal(Right.Id, selected.Value.Id);
        Assert.Equal(0, selected.Value.WorkingArea.Left);
    }

    [Fact]
    public void PhysicsUsesTheWorkingAreaOfTheDisplayContainingThePet()
    {
        var displays = new DisplaySnapshot(new[] { Left, Right });
        var surface = new DisplaySurface(displays, Right.WorkingArea);
        var pet = new PetInstance(TestSpriteFactory.LoadSpriteOnly("display"), new Vector2(-100, Left.WorkingArea.Bottom));
        pet.StartWalking(FacingDirection.Left);
        var engine = new PetEngine(surface, new FixedRandom());

        engine.Tick(pet, 0.1);

        Assert.InRange(pet.FootPosition.X, Left.WorkingArea.Left, Left.WorkingArea.Right);
        Assert.Equal(Left.WorkingArea.Bottom, pet.FootPosition.Y);
    }

    [Fact]
    public void NewInstancesUseTheSurfaceSpawnArea()
    {
        var displays = new DisplaySnapshot(new[] { Left, Right });
        var surface = new DisplaySurface(displays, Right.WorkingArea, Left.WorkingArea);
        using var session = new PetSession(surface, new FixedRandom());
        var definition = TestSpriteFactory.LoadSpriteOnly("preferred-spawn");
        session.RegisterDefinition(definition.Id, definition);

        var pet = session.Create(definition.Id);

        Assert.Equal(Left.WorkingArea.Left + 80, pet.FootPosition.X);
        Assert.Equal(Left.WorkingArea.Top - 32, pet.FootPosition.Y);
    }

    private sealed class DisplaySurface(IDisplayProvider displays, WorldRect fallback, WorldRect? spawn = null) : IWorldSurface
    {
        public WorldRect WorkingArea { get; } = fallback;
        public WorldRect SpawnArea { get; } = spawn ?? fallback;
        public bool IsValid => true;
        public IDisplayProvider DisplayProvider { get; } = displays;
    }

    private sealed class FixedRandom : IRandomSource
    {
        public double NextDouble() => 0.5;
        public int Next(int minInclusive, int maxExclusive) => minInclusive;
    }
}
