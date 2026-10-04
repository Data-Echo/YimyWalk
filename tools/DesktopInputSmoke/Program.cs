using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

internal static class Program
{
[STAThread]
private static void Main()
{
ApplicationConfiguration.Initialize();
var savedCursor = Cursor.Position;
using var probe = new ProbeForm();
probe.Bounds = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1280, 720);
probe.Show();
probe.Activate();
Pump(100);

var app = Assembly.Load("YimyWalk.App");
var hostType = app.GetType("YimyWalk.App.Host.AppHost", throwOnError: true)!;
var host = Activator.CreateInstance(hostType, nonPublic: true)!;
var hostDisposed = false;

try
{
    Invoke(hostType, host, "Start");
    Invoke(hostType, host, "CreateMany", "cat", 1);
    Pump(100);
    probe.Activate();
    Pump(50);
    Console.WriteLine($"FOCUS SETUP={Native.GetForegroundWindow()} PROBE={probe.Handle}");

    var session = Property(host, "Session");
    var windowsRegistry = Property(host, "Windows");
    var pet = First(Property(windowsRegistry, "Windows"));
    var petForm = (Form)pet;
    petForm.Activated += (_, _) => Console.WriteLine("EVENT PET_ACTIVATED");
    petForm.Deactivate += (_, _) => Console.WriteLine("EVENT PET_DEACTIVATED");
    petForm.MouseDown += (_, _) => Console.WriteLine($"EVENT MOUSE_DOWN state={State(Property(pet, "Instance"))} capture={petForm.Capture}");
    petForm.MouseCaptureChanged += (_, _) => Console.WriteLine($"EVENT CAPTURE={petForm.Capture}");
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
    if (Native.Describe(transparentRoot).Contains("class=LockScreenBackstopFrame") || Native.Describe(entityRoot).Contains("class=LockScreenBackstopFrame"))
    {
        Console.WriteLine($"RESULT UNVERIFIED INPUT_ROUTING_BLOCKED transparentRoot={Native.Describe(transparentRoot)} entityRoot={Native.Describe(entityRoot)}");
        Environment.ExitCode = 2;
        return;
    }
    Require(transparentRoot == probe.Handle, "transparent pixel passes to underlying probe");
    Require(entityRoot == petForm.Handle, "entity pixel hits pet window");
    Console.WriteLine($"HIT TRANSPARENT={transparentTarget} ENTITY={entityTarget} PET={petForm.Handle} PROBE={probe.Handle}");

    Console.WriteLine($"FOCUS BEFORE_PROBE_CLICK={Native.GetForegroundWindow()} PROBE={probe.Handle}");
    Native.SetCursorPos(transparentScreen.X, transparentScreen.Y);
    Native.MouseClick();
    Pump(100);
    Require(probe.Clicks > 0, "transparent click reached underlying probe");
    Console.WriteLine($"FOCUS AFTER_PROBE_CLICK={Native.GetForegroundWindow()} PROBE={probe.Handle}");
    Require(Native.GetForegroundWindow() == probe.Handle, "transparent click activates underlying probe");
    var foregroundAtHit = probe.Handle;

    var startFoot = Foot(instance);
    Native.SetCursorPos(opaqueScreen.X, opaqueScreen.Y);
    Native.MouseDown();
    Pump(80);
    Console.WriteLine($"DRAG DIAG state={State(instance)} capture={petForm.Capture} foreground={Native.GetForegroundWindow()} point={Cursor.Position} target={Native.WindowFromPoint(Cursor.Position)}");
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

    // The window moved during the first drag. Locate the entity in its new bounds.
    surface = PrivateField(pet, "_surface");
    alpha = (byte[])Property(surface, "Alpha");
    opaque = FindPixel(alpha, petForm.Size, a => a >= 12);
    Native.SetCursorPos(petForm.Left + opaque!.Value.X, petForm.Top + opaque.Value.Y);
    Native.MouseDown();
    Pump(50);
    Require(string.Equals(State(instance), "Dragged", StringComparison.Ordinal), "second drag starts");
    Native.ReleaseCapture();
    Native.MouseUp();
    Pump(100);
    Require(!string.Equals(State(instance), "Dragged", StringComparison.Ordinal), "capture loss releases drag");
    Console.WriteLine($"INPUT CAPTURE_LOSS_STATE={State(instance)}");

    probe.Activate();
    Pump(50);
    var foregroundBeforeVisibility = Native.GetForegroundWindow();
    Invoke(hostType, host, "SetPetsVisible", false);
    Pump(50);
    Require(!petForm.Visible, "tray hide action hides pet windows");
    Require(Native.GetForegroundWindow() == foregroundBeforeVisibility, "tray hide keeps foreground window");
    Invoke(hostType, host, "SetPetsVisible", true);
    Pump(50);
    Require(petForm.Visible, "tray show action restores pet windows");
    Console.WriteLine($"FOCUS VISIBILITY_BEFORE={foregroundBeforeVisibility} AFTER={Native.GetForegroundWindow()} PET={petForm.Handle} PROBE={probe.Handle}");
    Require(Native.GetForegroundWindow() == foregroundBeforeVisibility, "tray show keeps foreground window");

    var tray = PrivateField(host, "_tray");
    var icon = PrivateField(tray, "_icon");
    Require((bool)Property(icon, "Visible"), "tray icon is visible while host runs");
    Invoke(hostType, host, "ClearPets");
    Pump(100);
    Require((int)Property(session, "ActiveCount") == 0, "clear removes active instances");
    Console.WriteLine("LIFECYCLE HIDE_SHOW=PASS CLEAR=PASS");

    var manager = (Form)PrivateField(host, "_manager");
    ClickTrayMenuItem((NotifyIcon)icon, "打开管理器");
    Require(manager.Visible, "tray menu opens manager");
    manager.Hide();
    probe.Activate();
    Invoke(hostType, host, "CreateMany", "cat", 1);
    Pump(100);
    var menuPet = First(Property(windowsRegistry, "Windows"));
    var menuPetForm = (Form)menuPet;
    ClickTrayMenuItem((NotifyIcon)icon, "隐藏宠物");
    Require(!menuPetForm.Visible, "tray menu hides pets");
    ClickTrayMenuItem((NotifyIcon)icon, "显示宠物");
    Require(menuPetForm.Visible, "tray menu shows pets");
    ClickTrayMenuItem((NotifyIcon)icon, "清空宠物");
    Pump(100);
    Require((int)Property(session, "ActiveCount") == 0, "tray menu clears pets");
    Console.WriteLine("TRAY MENU_OPEN_HIDE_SHOW_CLEAR=PASS");

    ClickTrayMenuItem((NotifyIcon)icon, "退出");
    Pump(50);
    if (host is IDisposable exitedHost) exitedHost.Dispose();
    hostDisposed = true;
    Require(!(bool)Property(icon, "Visible"), "exit disposes tray icon");
    Console.WriteLine("LIFECYCLE EXIT_DISPOSE=PASS");
}
finally
{
    Native.MouseUp();
    if (!hostDisposed)
    {
        if (host is IDisposable disposable) disposable.Dispose();
        hostDisposed = true;
    }
    Pump(100);
    Cursor.Position = savedCursor;
}

Console.WriteLine("RESULT PASS");
}

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

static void ClickTrayMenuItem(NotifyIcon icon, string text)
{
    var menu = icon.ContextMenuStrip ?? throw new InvalidOperationException("tray menu is missing");
    var item = menu.Items.Cast<ToolStripItem>().SingleOrDefault(candidate => candidate.Text == text)
        ?? throw new InvalidOperationException($"tray menu item '{text}' is missing");
    var area = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1280, 720);
    menu.Show(new Point(area.Left + 30, area.Top + 30));
    Pump(50);
    var point = menu.PointToScreen(new Point(item.Bounds.Left + item.Bounds.Width / 2, item.Bounds.Top + item.Bounds.Height / 2));
    Native.SetCursorPos(point.X, point.Y);
    Native.MouseClick();
    Pump(100);
    Console.WriteLine($"TRAY MENU_CLICK={text}");
}
}

sealed class ProbeForm : Form
{
    public int Clicks { get; private set; }

    public ProbeForm()
    {
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        ShowInTaskbar = false;
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
