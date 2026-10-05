using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using YimyWalk.App.Host;
using YimyWalk.App.Rendering;
using YimyWalk.App.Windows;
using YimyWalk.Core.Characters;
using YimyWalk.Core.Simulation;

internal static class Program
{
    private static int _failures;
    private static int _checks;
    [STAThread]
    private static int Main(string[] args)
    {
        // Run the application's own initialization, so the smoke cannot mask an
        // application DPI-awareness bug by using a different bootstrap.
        YimyWalk.App.Program.InitializeDesktop();
        var savedCursor = Cursor.Position;
        try
        {
            Console.WriteLine($"ENV UTC={DateTime.UtcNow:O} OS={Environment.OSVersion} ARCH={RuntimeInformation.ProcessArchitecture} CPU_LOGICAL={Environment.ProcessorCount}");
            var screens = Screen.AllScreens;
            var adapter = new WindowsDisplayAdapter();
            var displays = adapter.Capture();
            foreach (var screen in screens)
            {
                var actualDpi = PhysicalDpi(screen.Bounds);
                var display = displays.Displays.Single(d => d.Id.Value == screen.DeviceName);
                Console.WriteLine($"DISPLAY {screen.DeviceName} PRIMARY={screen.Primary} BOUNDS={screen.Bounds} WORK={screen.WorkingArea} NATIVE_DPI={actualDpi} SNAPSHOT_SCALE={display.Scale}");
                Check(Math.Abs(display.Scale - actualDpi / 96f) < 0.01, "per-monitor DPI metadata " + screen.DeviceName);
            }
            Check(Native.AreDpiAwarenessContextsEqual(Native.GetThreadDpiAwarenessContext(), (nint)(-4)), "production startup PerMonitorV2");
            if (screens.Length < 2)
            {
                Console.WriteLine("RESULT UNVERIFIED TWO_EXTENDED_DISPLAYS_REQUIRED");
                return 2;
            }
            if (args.Contains("--inventory")) return Finish();
            if (args.Length == 0) throw new ArgumentException("Character root required");
            var rounds = ReadRounds(args);
            using var harness = new Harness(args[0], rounds);
            foreach (var screen in screens)
                harness.ExerciseScreen(screen);
            for (var i = 0; i < screens.Length; i++)
                harness.CrossScreen(screens[i], screens[(i + 1) % screens.Length]);
            harness.ResourceRounds(screens);
            Console.WriteLine("UNVERIFIED PHYSICAL_DISCONNECT_RECONNECT SLEEP_RESUME TASKBAR_CONFIGURATION_CHANGES");
            if (!screens.Any(s => s.Bounds.Left < 0 || s.Bounds.Top < 0))
                Console.WriteLine("UNVERIFIED NEGATIVE_COORDINATE_LAYOUT_NOT_PRESENT");
            return Finish();
        }
        catch (InputUnavailableException ex)
        {
            Console.WriteLine("RESULT UNVERIFIED " + ex.Message);
            return 2;
        }
        catch (Exception ex)
        {
            Console.WriteLine("RESULT FAIL " + ex);
            return 1;
        }
        finally
        {
            Native.mouse_event(4, 0, 0, 0, 0);
            Cursor.Position = savedCursor;
        }
    }

    private static int Finish()
    {
        Console.WriteLine($"RESULT {(_failures == 0 ? "PASS" : "FAIL")} CHECKS={_checks} FAILURES={_failures}");
        return _failures == 0 ? 0 : 1;
    }

    private static void Check(bool result, string message)
    {
        _checks++;
        if (!result) _failures++;
        Console.WriteLine($"{(result ? "PASS" : "FAIL")} {message}");
    }

    private static int ReadRounds(string[] args)
    {
        var index = Array.IndexOf(args, "--rounds");
        if (index < 0) return 3;
        if (index + 1 >= args.Length || !int.TryParse(args[index + 1], out var rounds) || rounds < 1 || rounds > 100)
            throw new ArgumentException("--rounds must be an integer from 1 to 100");
        return rounds;
    }

    private static uint PhysicalDpi(Rectangle bounds)
    {
        var old = Native.SetThreadDpiAwarenessContext((nint)(-4));
        try
        {
            using var window = new Form { StartPosition = FormStartPosition.Manual, Location = new Point(bounds.Left + 100, bounds.Top + 100), Size = new Size(32, 32) };
            return NativeMethods.GetDpiForWindow(window.Handle);
        }
        finally { Native.SetThreadDpiAwarenessContext(old); }
    }

    private sealed class Harness : IDisposable
    {
        private readonly DesktopWorldSurface _surface = new();
        private readonly FrameSurfaceCache _cache = new();
        private readonly PetSession _session;
        private readonly List<PetWindow> _windows = new();
        private readonly System.Windows.Forms.Timer _timer = new() { Interval = 16 };
        private readonly int _rounds;
        private long _lastTick = Stopwatch.GetTimestamp();

        public Harness(string root, int rounds)
        {
            _rounds = rounds;
            _session = new PetSession(_surface, new FixedRandom());
            foreach (var id in new[] { "cat", "slime" })
                _session.RegisterDefinition(id, CharacterLoader.LoadDirectory(Path.Combine(root, id)));
            _session.InstanceRemoved += RemoveWindow;
            _timer.Tick += Tick;
            _timer.Start();
        }

        private void Tick(object? sender, EventArgs e)
        {
            var now = Stopwatch.GetTimestamp();
            _session.Tick((now - _lastTick) / (double)Stopwatch.Frequency);
            _lastTick = now;
            foreach (var window in _windows) window.RefreshFrame();
        }

        private PetWindow Create(string id, Screen screen, float scale = 1)
        {
            _session.Resume();
            _surface.SetPreferredDisplay(screen.DeviceName);
            var instance = _session.Create(id);
            var window = new PetWindow(instance, _session, _cache, scale);
            _windows.Add(window);
            window.Show();
            window.RefreshFrame();
            return window;
        }

        public void ExerciseScreen(Screen screen)
        {
            Console.WriteLine("SCREEN_BEGIN " + screen.DeviceName);
            foreach (var role in new[] { "cat", "slime" })
            {
                var window = Create(role, screen);
                var instance = window.Instance;
                var start = instance.FootPosition;
                Check(start.Y < screen.WorkingArea.Top && start.X >= screen.WorkingArea.Left && start.X < screen.WorkingArea.Right, role + " spawns above selected screen");
                Wait(() => instance.State == PetState.Idle, 6000, "floor landing");
                Check(Math.Abs(instance.FootPosition.Y - screen.WorkingArea.Bottom) < 1, role + " lands at work-area floor");
                Check(Screen.FromHandle(window.Handle).DeviceName == screen.DeviceName, role + " native window on selected display");
                CheckScale(window, screen, 1);
                instance.StartWalking(FacingDirection.Right);
                var x = instance.FootPosition.X;
                Pump(250);
                Check(instance.FootPosition.X > x + 2, role + " real walk motion");
                instance.StartJump();
                var y = instance.FootPosition.Y;
                Pump(150);
                Check(instance.FootPosition.Y < y - 2, role + " real upward jump");
                Wait(() => instance.State == PetState.Idle, 3000, "jump return to floor");
                PositionForInput(window, screen);
                using var probe = ProbeBehind(window);
                CheckInput(window, probe);
                _session.Clear();
                Pump(60);
                Check(_windows.Count == 0 && _cache.Count == 0, role + " clear windows/cache");
            }
            var scaled = Create("cat", screen, 2);
            Wait(() => scaled.Instance.State == PetState.Idle, 6000, "scaled floor landing");
            CheckScale(scaled, screen, 2);
            _session.Clear();
        }

        private static void CheckScale(PetWindow window, Screen screen, float scale)
        {
            var dpi = PhysicalDpi(screen.Bounds);
            var expected = (int)Math.Round(window.Instance.Definition.CellWidth * scale * dpi / 96f);
            Check(window.Width == expected && window.Height == expected, $"render size {screen.DeviceName} global={scale} expected={expected} actual={window.Size} windowDpi={NativeMethods.GetDpiForWindow(window.Handle)}");
            var expectedFoot = new Vector2(window.Left + window.Instance.Definition.Anchor.X * scale * dpi / 96f,
                window.Top + window.Instance.Definition.Anchor.Y * scale * dpi / 96f);
            var surface = (RenderedSurface)typeof(PetWindow).GetField("_surface", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(window)!;
            Console.WriteLine($"SCALE_DIAG display={screen.DeviceName} requested={scale} dpi={dpi} native={NativeMethods.GetDpiForWindow(window.Handle)} bitmap={surface.Bitmap.Size} form={window.Size} location={window.Location}");
            Check(Vector2.Distance(window.Instance.FootPosition, expectedFoot) < 1.5f, "rendered foot matches physical position");
        }

        private void PositionForInput(PetWindow window, Screen screen)
        {
            var engine = new PetEngine(_surface, new FixedRandom());
            engine.BeginDrag(window.Instance);
            engine.SetDraggedFootPosition(window.Instance, new Vector2(screen.WorkingArea.Right - 120, screen.WorkingArea.Bottom - 100));
            _session.Pause();
            engine.ReleaseDrag(window.Instance, Vector2.Zero);
            Pump(100);
        }

        private static ProbeForm ProbeBehind(PetWindow window)
        {
            var probe = new ProbeForm { StartPosition = FormStartPosition.Manual, FormBorderStyle = FormBorderStyle.None,
                Bounds = new Rectangle(window.Left - 50, window.Top - 50, 180, 180), BackColor = Color.Beige, Text = "YimyWalk display test" };
            probe.Show();
            probe.Activate();
            Pump(100);
            Native.SetWindowPos(window.Handle, (nint)(-1), 0, 0, 0, 0, 0x0013 /* NOMOVE|NOSIZE|NOACTIVATE|SHOWWINDOW */);
            Pump(50);
            return probe;
        }

        private static Point Pixel(PetWindow window, bool opaque)
        {
            var surface = (RenderedSurface)typeof(PetWindow).GetField("_surface", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(window)!;
            for (var y = 2; y < surface.Bitmap.Height - 2; y++)
            for (var x = 2; x < surface.Bitmap.Width - 2; x++)
                if ((surface.Alpha[y * surface.Bitmap.Width + x] >= 12) == opaque)
                    return new Point(window.Left + x, window.Top + y);
            throw new InvalidOperationException("No suitable pixel found");
        }

        private static void CheckInput(PetWindow window, ProbeForm probe)
        {
            var transparent = Pixel(window, false);
            var opaque = Pixel(window, true);
            Native.SetCursorPos(transparent.X, transparent.Y);
            var target = Native.WindowFromPoint(transparent);
            var transparentHit = Native.SendHitTest(window.Handle, transparent);
            var name = new StringBuilder(128);
            NativeMethods.GetClassName(target, name, name.Capacity);
            if (name.ToString().Contains("LockScreen")) throw new InputUnavailableException("LOCKED_DESKTOP");
            Check(target == probe.Handle, "transparent pixel routes to underlying probe");
            var clicks = probe.Clicks;
            Native.mouse_event(2, 0, 0, 0, 0);
            Native.mouse_event(4, 0, 0, 0, 0);
            Pump(80);
            Check(probe.Clicks > clicks, "real transparent click reached probe");
            Native.SetCursorPos(opaque.X, opaque.Y);
            var opaqueTarget = Native.WindowFromPoint(opaque);
            var opaqueHit = Native.SendHitTest(window.Handle, opaque);
            Console.WriteLine($"INPUT_DIAG PET={window.Handle} PROBE={probe.Handle} BOUNDS={window.Bounds} TRANSPARENT={transparent} TARGET={Native.Describe(target)} HIT={transparentHit} OPAQUE={opaque} TARGET={Native.Describe(opaqueTarget)} HIT={opaqueHit} FOREGROUND={NativeMethods.GetForegroundWindow()}");
            if (target != probe.Handle && target != window.Handle)
                throw new InputUnavailableException("INPUT_ROUTING_BLOCKED transparentTarget=" + Native.Describe(target));
            if (opaqueTarget != probe.Handle && opaqueTarget != window.Handle)
                throw new InputUnavailableException("INPUT_ROUTING_BLOCKED opaqueTarget=" + Native.Describe(opaqueTarget));
            Check(opaqueTarget == window.Handle, "opaque pixel routes to pet");
            Native.mouse_event(2, 0, 0, 0, 0);
            Pump(80);
            Check(window.Instance.State == PetState.Dragged && window.Capture, "real entity click captures drag");
            Check(NativeMethods.GetForegroundWindow() == probe.Handle, "pet click does not steal focus");
            Native.mouse_event(4, 0, 0, 0, 0);
            Pump(50);
            Check(window.Instance.State != PetState.Dragged && !window.Capture, "mouse release ends capture");
        }

        public void CrossScreen(Screen source, Screen destination)
        {
            Console.WriteLine($"CROSS_BEGIN {source.DeviceName} -> {destination.DeviceName}");
            var window = Create("cat", source);
            PositionForInput(window, source);
            using var probe = ProbeBehind(window);
            var grab = Pixel(window, true);
            Native.SetCursorPos(grab.X, grab.Y);
            Native.mouse_event(2, 0, 0, 0, 0);
            Pump(50);
            if (window.Instance.State != PetState.Dragged) throw new InputUnavailableException("DRAG_NOT_RECEIVED");
            var target = new Point(destination.WorkingArea.Left + 300, destination.WorkingArea.Top + 300);
            for (var step = 1; step <= 20; step++)
            {
                Native.SetCursorPos(grab.X + (target.X - grab.X) * step / 20, grab.Y + (target.Y - grab.Y) * step / 20);
                Native.mouse_event(1, 0, 0, 0, 0);
                Pump(30);
            }
            Pump(100);
            Check(Screen.FromHandle(window.Handle).DeviceName == destination.DeviceName, "drag reaches destination native display");
            Check(window.Bounds.Contains(Cursor.Position), "cursor remains on pet during DPI transition");
            CheckScale(window, destination, 1);
            Check(NativeMethods.GetForegroundWindow() == probe.Handle, "cross-screen drag preserves foreground");
            Native.mouse_event(4, 0, 0, 0, 0);
            Pump(30);
            Check(window.Instance.State != PetState.Dragged && !window.Capture, "cross-screen release ends drag");
            _session.Resume();
            Wait(() => window.Instance.State == PetState.Idle, 6000, "cross-screen landing");
            Check(Math.Abs(window.Instance.FootPosition.Y - destination.WorkingArea.Bottom) < 1, "cross-screen release lands at destination floor");
            Console.WriteLine($"CROSS_END FOOT={window.Instance.FootPosition} BOUNDS={window.Bounds}");
            _session.Clear();
        }

        public void ResourceRounds(Screen[] screens)
        {
            _session.Resume();
            for (var round = 1; round <= _rounds; round++)
            {
                using var process = Process.GetCurrentProcess();
                var cpuStart = process.TotalProcessorTime;
                var time = Stopwatch.StartNew();
                foreach (var screen in screens) for (var i = 0; i < 5; i++) Create(i % 2 == 0 ? "cat" : "slime", screen);
                Pump(2000);
                Check(_session.ActiveCount == screens.Length * 5 && _windows.All(w => w.Visible), "actual visible instances per round");
                Metrics($"ROUND={round} ACTIVE", _session.ActiveCount);
                Console.WriteLine($"CPU NORMALIZED_PERCENT={(process.TotalProcessorTime - cpuStart).TotalSeconds / time.Elapsed.TotalSeconds / Environment.ProcessorCount * 100:0.00}");
                _session.Clear();
                Pump(100);
                Check(_windows.Count == 0 && _cache.Count == 0, "round clear windows/cache");
                Metrics($"ROUND={round} CLEAR", _session.ActiveCount);
            }
        }

        private void RemoveWindow(PetInstance pet)
        {
            var window = _windows.Single(w => w.Instance == pet);
            _windows.Remove(window);
            window.Dispose();
        }
        public void Dispose()
        {
            _timer.Stop();
            _timer.Tick -= Tick;
            _timer.Dispose();
            _session.Dispose();
            _session.InstanceRemoved -= RemoveWindow;
            _cache.Dispose();
            _surface.Dispose();
            Pump(50);
            Metrics("DISPOSE", 0);
        }
    }

    private static void Metrics(string phase, int count)
    {
        using var process = Process.GetCurrentProcess();
        process.Refresh();
        Console.WriteLine($"METRIC {phase} ACTIVE={count} WS={process.WorkingSet64} PRIVATE={process.PrivateMemorySize64} HANDLES={process.HandleCount} USER={Native.GetGuiResources(process.Handle, 1)} GDI={Native.GetGuiResources(process.Handle, 0)}");
    }
    private static void Pump(int milliseconds)
    {
        var watch = Stopwatch.StartNew();
        while (watch.ElapsedMilliseconds < milliseconds) { Application.DoEvents(); Thread.Sleep(5); }
    }
    private static void Wait(Func<bool> predicate, int milliseconds, string message)
    {
        var watch = Stopwatch.StartNew();
        while (!predicate() && watch.ElapsedMilliseconds < milliseconds) Pump(10);
        if (!predicate()) throw new InvalidOperationException("Timed out: " + message);
    }
    private sealed class FixedRandom : IRandomSource
    {
        public double NextDouble() => 0;
        public int Next(int minInclusive, int maxExclusive) => minInclusive;
    }
    private sealed class ProbeForm : Form
    {
        public int Clicks { get; private set; }
        protected override void OnMouseDown(MouseEventArgs e) { Clicks++; base.OnMouseDown(e); }
    }
    private sealed class InputUnavailableException(string message) : Exception(message);
    private static class Native
    {
        [DllImport("user32.dll")] public static extern nint GetThreadDpiAwarenessContext();
        [DllImport("user32.dll")] public static extern nint SetThreadDpiAwarenessContext(nint context);
        [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] public static extern bool AreDpiAwarenessContextsEqual(nint a, nint b);
        [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
        [DllImport("user32.dll")] public static extern nint WindowFromPoint(Point point);
        [DllImport("user32.dll")] public static extern bool SetWindowPos(nint hwnd, nint insertAfter, int x, int y, int width, int height, uint flags);
        [DllImport("user32.dll")] public static extern nint SendMessage(nint hwnd, uint message, nint wParam, nint lParam);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetClassName(nint hwnd, StringBuilder text, int capacity);
        [DllImport("user32.dll")] private static extern bool GetWindowRect(nint hwnd, out RECT rect);
        public static nint SendHitTest(nint hwnd, Point point) => SendMessage(hwnd, 0x0084, 0, (nint)((point.Y << 16) | (point.X & 0xffff)));
        public static string Describe(nint hwnd)
        {
            var text = new StringBuilder(128);
            GetClassName(hwnd, text, text.Capacity);
            return $"{hwnd} class={text} rect={(GetWindowRect(hwnd, out var rect) ? rect.ToString() : "<none>")}";
        }
        [DllImport("user32.dll")] public static extern void mouse_event(uint flags, uint dx, uint dy, uint data, nuint extra);
        [DllImport("user32.dll")] public static extern uint GetGuiResources(nint process, uint flag);
        [StructLayout(LayoutKind.Sequential)] private struct RECT { public int Left, Top, Right, Bottom; public override string ToString() => $"({Left},{Top})-({Right},{Bottom})"; }
    }
}
