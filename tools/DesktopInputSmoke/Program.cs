using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

const string PublishPath = "src/YimyWalk.App/bin/x64/Release/net8.0-windows/win-x64/publish/YimyWalk.App.dll";

ApplicationConfiguration.Initialize();
using var probe = new ProbeForm();
probe.Bounds = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1280, 720);
probe.Show();
probe.Activate();
Pump(100);

var foregroundBeforePet = Native.GetForegroundWindow();
var app = Assembly.LoadFrom(Path.GetFullPath(PublishPath));
var hostType = app.GetType("YimyWalk.App.Host.AppHost", throwOnError: true)!;
var host = Activator.CreateInstance(hostType, nonPublic: true)!;
var hostDisposed = false;

try
{
    Invoke(hostType, host, "Start");
    Invoke(hostType, host, "CreateMany", "cat", 1);
    Pump(100);

    var session = Property(host, "Session");
    var windowsRegistry = Property(host, "Windows");
    var pet = First(Property(windowsRegistry, "Windows"));
    var petForm = (Form)pet;
    var instance = Property(pet, "Instance");
    var initialY = Foot(instance).Y;
    WaitUntil(() => string.Equals(State(instance), "Idle", StringComparison.Ordinal), 5000, "fall-to-floor");
    var floorY = Foot(instance).Y;
    Console.WriteLine($"MOTION FALL_START_Y={initialY:0.0} FLOOR_Y={floorY:0.0} STATE={State(instance)}");

    Invoke(instance.GetType(), instance, "StartWalking", Enum.Parse(Property(instance, "Facing").GetType(), "Right"));
    var walkStartX = Foot(instance).X;
    WaitUntil(() => Math.Abs(Foot(instance).X - walkStartX) > 2, 1500, "walk-motion");
    Console.WriteLine($"MOTION WALK_DELTA_X={Foot(instance).X - walkStartX:0.0} STATE={State(instance)}");

    Invoke(instance.GetType(), instance, "StartJump");
    var jumpStates = new HashSet<string>(StringComparer.Ordinal);
    var jumpDeadline = Stopwatch.GetTimestamp() + Stopwatch.Frequency * 3;
    while (Stopwatch.GetTimestamp() < jumpDeadline)
    {
        jumpStates.Add(State(instance));
        Pump(16);
        if (string.Equals(State(instance), "Idle", StringComparison.Ordinal) && jumpStates.Contains("Jump")) break;
    }
    jumpStates.Add(State(instance));
    Require(jumpStates.Contains("Jump"), "jump state observed");
    Require(jumpStates.Contains("Fall"), "fall state observed after jump");
    Console.WriteLine($"MOTION JUMP_STATES={string.Join(',', jumpStates.OrderBy(x => x))}");

    Invoke(session.GetType(), session, "Pause");
    Pump(50);
    var surface = PrivateField(pet, "_surface");
    var alpha = (byte[])Property(surface, "Alpha");
    var bounds = petForm.Bounds;
    var opaque = FindPixel(alpha, bounds.Size, a => a >= 12);
    var transparent = FindPixel(alpha, bounds.Size, a => a < 12);
    Require(opaque is not null, "an opaque entity pixel exists");
    Require(transparent is not null, "a transparent pixel inside the window exists");

    var opaqueScreen = new Point(bounds.Left + opaque!.Value.X, bounds.Top + opaque.Value.Y);
    var transparentScreen = new Point(bounds.Left + transparent!.Value.X, bounds.Top + transparent.Value.Y);
    // WindowFromPoint represents a real pointer query, so place the real cursor
    // at each probe point before asking Windows to route it.
    Native.SetCursorPos(transparentScreen.X, transparentScreen.Y);
    var transparentTarget = Native.WindowFromPoint(transparentScreen);
    Native.SetCursorPos(opaqueScreen.X, opaqueScreen.Y);
    var entityTarget = Native.WindowFromPoint(opaqueScreen);
    Console.WriteLine($"DIAG BOUNDS={bounds} OPAQUE={opaqueScreen} TRANSPARENT={transparentScreen} ALPHA={alpha[transparent!.Value.Y * bounds.Width + transparent.Value.X]} TARGETS={transparentTarget}/{entityTarget} ROOTS={Native.GetAncestor(transparentTarget, Native.GA_ROOT)}/{Native.GetAncestor(entityTarget, Native.GA_ROOT)} PET={petForm.Handle} PROBE={probe.Handle}");
    Console.WriteLine($"DIAG TARGET_INFO={Native.Describe(transparentTarget)} ROOT_INFO={Native.Describe(Native.GetAncestor(transparentTarget, Native.GA_ROOT))}");
    Native.SetCursorPos(0, 0);
    var directTransparentHit = Native.SendHitTest(petForm.Handle, transparentScreen);
    var directEntityHit = Native.SendHitTest(petForm.Handle, opaqueScreen);
    Console.WriteLine($"DIAG HITTEST_TRANSPARENT={directTransparentHit}");
    Console.WriteLine($"DIAG HITTEST_ENTITY={directEntityHit}");
    Require(directTransparentHit == (IntPtr)(-1), "message coordinates identify transparent pixel");
    Require(directEntityHit == (IntPtr)1, "message coordinates identify entity pixel");

    var transparentRoot = Native.GetAncestor(transparentTarget, Native.GA_ROOT);
    var entityRoot = Native.GetAncestor(entityTarget, Native.GA_ROOT);
    if (transparentRoot != probe.Handle || entityRoot != petForm.Handle)
    {
        Console.WriteLine($"RESULT UNVERIFIED INPUT_ROUTING_BLOCKED transparentRoot={Native.Describe(transparentRoot)} entityRoot={Native.Describe(entityRoot)}");
        Environment.ExitCode = 2;
        return;
    }
    Require(transparentRoot == probe.Handle, "transparent pixel passes to underlying probe");
    Require(entityRoot == petForm.Handle, "entity pixel hits pet window");
    Console.WriteLine($"HIT TRANSPARENT={transparentTarget} ENTITY={entityTarget} PET={petForm.Handle} PROBE={probe.Handle}");

    var foregroundAtHit = Native.GetForegroundWindow();
    Native.SetCursorPos(transparentScreen.X, transparentScreen.Y);
    Native.MouseClick();
    Pump(100);
    Require(probe.Clicks > 0, "transparent click reached underlying probe");
    Require(Native.GetForegroundWindow() == foregroundAtHit, "transparent click keeps foreground window");

    var startFoot = Foot(instance);
    Native.SetCursorPos(opaqueScreen.X, opaqueScreen.Y);
    Native.MouseDown();
    Pump(80);
    Require(string.Equals(State(instance), "Dragged", StringComparison.Ordinal), "entity mouse down starts drag");
    Require(Native.GetForegroundWindow() == foregroundAtHit, "entity drag does not activate pet window");
    var dragPoint = new Point(opaqueScreen.X + 120, opaqueScreen.Y - 80);
    Native.SetCursorPos(dragPoint.X, dragPoint.Y);
    Native.MouseMove();
    Pump(80);
    var draggedFoot = Foot(instance);
    Require(Math.Abs(draggedFoot.X - startFoot.X) > 20 || Math.Abs(draggedFoot.Y - startFoot.Y) > 20, "drag moves the entity");
    Native.MouseUp();
    Pump(80);
    Require(!string.Equals(State(instance), "Dragged", StringComparison.Ordinal), "mouse release ends drag");
    Console.WriteLine($"INPUT DRAG_START={startFoot.X:0.0},{startFoot.Y:0.0} DRAG_END={draggedFoot.X:0.0},{draggedFoot.Y:0.0} RELEASE_STATE={State(instance)}");

    Native.SetCursorPos(opaqueScreen.X, opaqueScreen.Y);
    Native.MouseDown();
    Pump(50);
    Require(string.Equals(State(instance), "Dragged", StringComparison.Ordinal), "second drag starts");
    Native.ReleaseCapture();
    Pump(100);
    Require(!string.Equals(State(instance), "Dragged", StringComparison.Ordinal), "capture loss releases drag");
    Console.WriteLine($"INPUT CAPTURE_LOSS_STATE={State(instance)}");

    Invoke(hostType, host, "SetPetsVisible", false);
    Pump(50);
    Require(!petForm.Visible, "tray hide action hides pet windows");
    Invoke(hostType, host, "SetPetsVisible", true);
    Pump(50);
    Require(petForm.Visible, "tray show action restores pet windows");

    var tray = PrivateField(host, "_tray");
    var icon = PrivateField(tray, "_icon");
    Require((bool)Property(icon, "Visible"), "tray icon is visible while host runs");
    Invoke(hostType, host, "ClearPets");
    Pump(100);
    Require((int)Property(session, "ActiveCount") == 0, "clear removes active instances");
    Console.WriteLine("LIFECYCLE HIDE_SHOW=PASS CLEAR=PASS");
}
finally
{
    if (!hostDisposed)
    {
        if (host is IDisposable disposable) disposable.Dispose();
        hostDisposed = true;
    }
    Pump(100);
}

Require(Native.GetForegroundWindow() == foregroundBeforePet || foregroundBeforePet == IntPtr.Zero,
    "host disposal leaves foreground ownership outside pet windows");
Console.WriteLine("RESULT PASS");

static object Invoke(Type type, object target, string name, params object[] args)
    => type.GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(target, args)!;

static object Property(object target, string name)
    => target.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(target)!;

static object PrivateField(object target, string name)
    => target.GetType().GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(target)!;

static object First(object value)
    => ((System.Collections.IEnumerable)value).Cast<object>().First();

static string State(object instance) => Property(instance, "State").ToString()!;

static PointF Foot(object instance)
{
    var value = Property(instance, "FootPosition");
    return new PointF((float)value.GetType().GetField("X")!.GetValue(value)!, (float)value.GetType().GetField("Y")!.GetValue(value)!);
}

static Point? FindPixel(byte[] alpha, Size size, Func<byte, bool> predicate)
{
    for (var y = 0; y < size.Height; y++)
    for (var x = 0; x < size.Width; x++)
        if (predicate(alpha[y * size.Width + x])) return new Point(x, y);
    return null;
}

static void WaitUntil(Func<bool> condition, int timeoutMs, string name)
{
    var deadline = Environment.TickCount64 + timeoutMs;
    while (!condition() && Environment.TickCount64 < deadline) Pump(16);
    Require(condition(), name);
}

static void Pump(int milliseconds)
{
    var deadline = Environment.TickCount64 + milliseconds;
    do
    {
        Application.DoEvents();
        Thread.Sleep(2);
    } while (Environment.TickCount64 < deadline);
}

static void Require(bool condition, string description)
{
    if (!condition) throw new InvalidOperationException($"FAIL: {description}");
}

sealed class ProbeForm : Form
{
    public int Clicks { get; private set; }

    public ProbeForm()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        BackColor = Color.CornflowerBlue;
        MouseDown += (_, e) => { if (e.Button == MouseButtons.Left) Clicks++; };
    }
}

static class Native
{
    public const uint MOUSEEVENTF_MOVE = 0x0001;
    public const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    public const uint MOUSEEVENTF_LEFTUP = 0x0004;
    public const uint GA_ROOT = 2;

    [StructLayout(LayoutKind.Sequential)] public struct POINT { public int X, Y; }
    [DllImport("user32.dll")] public static extern IntPtr WindowFromPoint(POINT point);
    [DllImport("user32.dll")] public static extern IntPtr GetAncestor(IntPtr hwnd, uint flags);
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] public static extern IntPtr SendMessage(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetClassName(IntPtr hwnd, StringBuilder text, int capacity);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetWindowText(IntPtr hwnd, StringBuilder text, int capacity);
    [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr hwnd, out RECT rect);
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern void mouse_event(uint flags, uint dx, uint dy, uint data, UIntPtr extraInfo);
    [DllImport("user32.dll")] public static extern bool ReleaseCapture();

    public static IntPtr WindowFromPoint(Point point) => WindowFromPoint(new POINT { X = point.X, Y = point.Y });
    public static IntPtr SendHitTest(IntPtr hwnd, Point point)
    {
        var lParam = (IntPtr)((point.Y << 16) | (point.X & 0xffff));
        return SendMessage(hwnd, 0x0084, IntPtr.Zero, lParam);
    }
    public static void MouseDown() => mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
    public static void MouseUp() => mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
    public static void MouseMove() => mouse_event(MOUSEEVENTF_MOVE, 0, 0, 0, UIntPtr.Zero);
    public static void MouseClick() { MouseDown(); MouseUp(); }
    public static string Describe(IntPtr hwnd)
    {
        var klass = new StringBuilder(256);
        var title = new StringBuilder(256);
        GetClassName(hwnd, klass, klass.Capacity);
        GetWindowText(hwnd, title, title.Capacity);
        return $"{hwnd} class={klass} title={title} rect={(GetWindowRect(hwnd, out var rect) ? rect.ToString() : "<none>")}";
    }
    [StructLayout(LayoutKind.Sequential)] private struct RECT { public int Left, Top, Right, Bottom; public override string ToString() => $"({Left},{Top})-({Right},{Bottom})"; }
}
