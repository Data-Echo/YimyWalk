using System.ComponentModel;
using System.Runtime.InteropServices;
using YimyWalk.App.Windows;
using YimyWalk.Core.Display;
using YimyWalk.Core.Simulation;

namespace YimyWalk.App.Host;

/// <summary>
/// Converts the current Windows monitor topology into a Core display snapshot.
/// It only reads monitor and DPI information; it never changes display settings.
/// </summary>
internal sealed class WindowsDisplayAdapter
{
    private const uint DefaultDpi = 96;

    public DisplaySnapshot Capture(IntPtr windowHandle = default)
    {
        try
        {
            var screens = Screen.AllScreens;
            var displays = new List<DisplayInfo>(screens.Length);
            for (var index = 0; index < screens.Length; index++)
            {
                var screen = screens[index];
                var bounds = ToWorldRect(screen.Bounds);
                var workingArea = ToWorldRect(screen.WorkingArea);
                var id = string.IsNullOrWhiteSpace(screen.DeviceName)
                    ? $"DISPLAY{index + 1}"
                    : screen.DeviceName;
                displays.Add(new DisplayInfo(
                    new DisplayId(id),
                    bounds,
                    workingArea,
                    ReadMonitorScale(screen),
                    screen.Primary));
            }

            if (displays.Count > 0) return new DisplaySnapshot(displays);
        }
        catch (Exception ex) when (ex is InvalidOperationException or ExternalException or Win32Exception)
        {
            // Monitor enumeration can fail during a display reconnect. The
            // deterministic fallback keeps physics and startup usable.
        }

        return new DisplaySnapshot(new[]
        {
            new DisplayInfo(
                new DisplayId("DISPLAY-FALLBACK"),
                new WorldRect(0, 0, 1280, 720),
                new WorldRect(0, 0, 1280, 720),
                1f,
                IsPrimary: true)
        });
    }

    public float ScaleForWindow(IntPtr windowHandle)
        => ReadScale(windowHandle);

    private static float ReadMonitorScale(Screen screen)
    {
        try
        {
            var center = new NativeMethods.Point(screen.Bounds.Left + screen.Bounds.Width / 2,
                screen.Bounds.Top + screen.Bounds.Height / 2);
            var monitor = MonitorFromPoint(center, 2 /* MONITOR_DEFAULTTONEAREST */);
            if (monitor != IntPtr.Zero && GetDpiForMonitor(monitor, 0 /* MDT_EFFECTIVE_DPI */, out var x, out _) == 0 && x > 0)
                return x / (float)DefaultDpi;
        }
        catch (EntryPointNotFoundException) { }
        catch (DllNotFoundException) { }
        return ReadScale(IntPtr.Zero);
    }

    [DllImport("user32.dll")] private static extern IntPtr MonitorFromPoint(NativeMethods.Point point, uint flags);
    [DllImport("shcore.dll")] private static extern int GetDpiForMonitor(IntPtr monitor, int type, out uint dpiX, out uint dpiY);

    private static float ReadScale(IntPtr windowHandle)
    {
        try
        {
            var dpi = windowHandle != IntPtr.Zero
                ? NativeMethods.GetDpiForWindow(windowHandle)
                : NativeMethods.GetDpiForSystem();
            if (dpi > 0) return dpi / (float)DefaultDpi;
        }
        catch (EntryPointNotFoundException)
        {
            // Older Windows versions fall back to the logical 96 DPI scale.
        }
        catch (DllNotFoundException)
        {
            // The App target is Windows; retain a safe value for diagnostics.
        }

        return 1f;
    }

    private static WorldRect ToWorldRect(Rectangle rectangle)
        => new(rectangle.Left, rectangle.Top, rectangle.Right, rectangle.Bottom);
}
