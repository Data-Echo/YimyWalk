using YimyWalk.Core.Simulation;

namespace YimyWalk.App.Host;

internal sealed class DesktopWorldSurface : IWorldSurface
{
    public WorldRect WorkingArea
    {
        get
        {
            var area = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1280, 720);
            return new WorldRect(area.Left, area.Top, area.Right, area.Bottom);
        }
    }

    public bool IsValid => Screen.PrimaryScreen is not null;
}
