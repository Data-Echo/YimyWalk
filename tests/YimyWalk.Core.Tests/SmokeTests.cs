namespace YimyWalk.Core.Tests;

public sealed class SmokeTests
{
    [Fact]
    public void CoreAssemblyDoesNotReferenceWindowsForms()
    {
        var assembly = typeof(YimyWalk.Core.Characters.CharacterDefinition).Assembly;
        Assert.DoesNotContain(assembly.GetReferencedAssemblies(), x =>
            x.Name is "System.Windows.Forms" or "WindowsBase" or "PresentationCore");
    }
}
