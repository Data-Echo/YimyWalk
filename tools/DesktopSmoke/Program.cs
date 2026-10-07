using System.Reflection;
using System.Runtime.InteropServices;
using System.Numerics;
using System.Windows.Forms;
using YimyWalk.Core.Simulation;

var rounds = args.Length == 0 ? 1 : int.TryParse(args[0], out var parsed) && parsed > 0 && parsed <= 20 ? parsed : throw new ArgumentException("Rounds must be between 1 and 20.");
var role = args.Length < 2 ? "slime" : args[1];
var landingRequested = args.Any(arg => arg.Equals("landing", StringComparison.OrdinalIgnoreCase));
var targetedRequested = args.Any(arg => arg.Equals("targeted", StringComparison.OrdinalIgnoreCase));
var appAssemblyCandidates = new[]
{
    Path.GetFullPath("src/YimyWalk.App/bin/x64/Release/net8.0-windows/YimyWalk.App.dll"),
    Path.GetFullPath("src/YimyWalk.App/bin/x64/Release/net8.0-windows/win-x64/YimyWalk.App.dll"),
    Path.GetFullPath("src/YimyWalk.App/bin/x64/Release/net8.0-windows/win-x64/publish/YimyWalk.App.dll")
};
var appAssemblyPath = appAssemblyCandidates.FirstOrDefault(File.Exists)
    ?? throw new FileNotFoundException("YimyWalk.App development assembly was not found.");
var app = Assembly.LoadFrom(appAssemblyPath);
var settingsRoot = Path.Combine(Path.GetTempPath(), "YimyWalkSettingsSmoke", Guid.NewGuid().ToString("N"));
var settingsType = app.GetType("YimyWalk.App.Settings.SettingsStore", true)!;
var settingsStore = Activator.CreateInstance(settingsType, new object[] { settingsRoot })!;
var settingsLoad = settingsType.GetMethod("Load")!.Invoke(settingsStore, Array.Empty<object>())!;
var settingsResultType = settingsLoad.GetType();
if (!(bool)settingsResultType.GetProperty("UsedDefaults")!.GetValue(settingsLoad)!)
    throw new InvalidOperationException("missing settings did not use defaults");
var settingsDocument = Assembly.Load("YimyWalk.Core").GetType("YimyWalk.Core.Settings.SettingsDocument", true)!;
var defaults = settingsDocument.GetProperty("Defaults")!.GetValue(null)!;
settingsType.GetMethod("Save")!.Invoke(settingsStore, new[] { defaults });
settingsType.GetMethod("Save")!.Invoke(settingsStore, new[] { defaults });
var savedLoad = settingsType.GetMethod("Load")!.Invoke(settingsStore, Array.Empty<object>())!;
if ((bool)savedLoad.GetType().GetProperty("UsedDefaults")!.GetValue(savedLoad)!)
    throw new InvalidOperationException("saved settings did not load");
var settingsPath = (string)settingsType.GetProperty("SettingsPath")!.GetValue(settingsStore)!;
File.WriteAllBytes(settingsPath, new byte[] { 1, 2, 3 });
var recoveredLoad = settingsType.GetMethod("Load")!.Invoke(settingsStore, Array.Empty<object>())!;
if ((bool)recoveredLoad.GetType().GetProperty("UsedDefaults")!.GetValue(recoveredLoad)!)
    throw new InvalidOperationException("backup settings did not recover");
if (!(bool)recoveredLoad.GetType().GetProperty("UsedBackup")!.GetValue(recoveredLoad)!)
    throw new InvalidOperationException("backup settings recovery was not reported");
Console.WriteLine("SETTINGS STORE DEFAULT_SAVE_BACKUP=PASS");
Directory.Delete(settingsRoot, recursive: true);
var type = app.GetType("YimyWalk.App.Host.AppHost", true)!;
var host = Activator.CreateInstance(type, nonPublic: true)!;
var characterTemp = Path.Combine(Path.GetTempPath(), "YimyWalkDesktopSmoke-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(characterTemp);
CopyCharacterFixture("slime", characterTemp);
CopyCharacterFixture("cat", characterTemp);
type.GetProperty("CharacterRootOverride", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.SetValue(host, characterTemp);
const string iconResourceName = "YimyWalk.App.Resources.yimywalk.ico";
if (!app.GetManifestResourceNames().Contains(iconResourceName, StringComparer.Ordinal))
    throw new InvalidOperationException($"application icon resource is not embedded: {iconResourceName}");
var appIconType = app.GetType("YimyWalk.App.UI.AppIcon", true)!;
using var expectedIcon = (Icon)appIconType.GetMethod("Load", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!
    .Invoke(null, null)!;
var manager = type.GetField("_manager", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(host)
    ?? throw new MissingFieldException(type.FullName, "_manager");
var managerIcon = manager.GetType().GetProperty("Icon")?.GetValue(manager) as Icon
    ?? throw new InvalidOperationException("manager form did not load the application icon");
var tray = type.GetField("_tray", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(host)
    ?? throw new MissingFieldException(type.FullName, "_tray");
var trayIconHostIcon = tray.GetType().GetField("_icon", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(tray)
    ?? throw new MissingFieldException(tray.GetType().FullName, "_icon");
var trayIcon = trayIconHostIcon.GetType().GetProperty("Icon")?.GetValue(trayIconHostIcon) as Icon
    ?? throw new InvalidOperationException("tray icon did not load the application icon");
if (!SameIcon(expectedIcon, managerIcon) || !SameIcon(expectedIcon, trayIcon))
    throw new InvalidOperationException($"manager/tray icons do not match the embedded application icon: expected={expectedIcon.Size}, manager={managerIcon.Size}, tray={trayIcon.Size}");
Console.WriteLine($"APPLICATION ICON RESOURCE={iconResourceName} SIZE={expectedIcon.Size} MANAGER=PASS TRAY=PASS");
var session = type.GetProperty("Session")!.GetValue(host)!;
var windows = type.GetProperty("Windows")!.GetValue(host)!;
var active = session.GetType().GetProperty("ActiveCount")!;
var windowCount = windows.GetType().GetProperty("Count")!;
var cachedSurfaceCount = type.GetProperty("CachedSurfaceCount")!;
type.GetMethod("Start")!.Invoke(host, Array.Empty<object>());
var displayProvider = type.GetProperty("DisplayProvider")!.GetValue(host)!;
var displayValues = ((System.Collections.IEnumerable)displayProvider.GetType().GetProperty("Displays")!.GetValue(displayProvider)!).Cast<object>().ToArray();
Console.WriteLine($"DISPLAY COUNT={displayValues.Length}");
foreach (var display in displayValues)
{
    var id = display.GetType().GetProperty("Id")!.GetValue(display)!;
    var bounds = display.GetType().GetProperty("Bounds")!.GetValue(display)!;
    var workingArea = display.GetType().GetProperty("WorkingArea")!.GetValue(display)!;
    var scale = display.GetType().GetProperty("Scale")!.GetValue(display)!;
    var primary = display.GetType().GetProperty("IsPrimary")!.GetValue(display)!;
    Console.WriteLine($"DISPLAY ID={id} BOUNDS={bounds} WORKING={workingArea} SCALE={scale} PRIMARY={primary}");
}

var refreshWindowSurfaces = type.GetMethod("RefreshWindowSurfaces", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
    ?? throw new MissingMethodException(type.FullName, "RefreshWindowSurfaces");
if (targetedRequested)
{
    using (var maximizedProbe = new Form
    {
        FormBorderStyle = FormBorderStyle.None,
        ShowInTaskbar = false,
        WindowState = FormWindowState.Maximized,
        BackColor = Color.CornflowerBlue,
        Text = "YimyWalk maximized landing probe"
    })
    {
        maximizedProbe.Show();
        Application.DoEvents();
        refreshWindowSurfaces.Invoke(host, Array.Empty<object>());
        var candidates = ((System.Collections.IEnumerable)type.GetProperty("WindowSurfaceCandidates")!.GetValue(host)!)
            .Cast<object>()
            .ToArray();
        var probeId = maximizedProbe.Handle.ToInt64().ToString("X");
        var probeCandidate = candidates.SingleOrDefault(candidate =>
            string.Equals(Property(candidate, "Id").ToString(), probeId, StringComparison.OrdinalIgnoreCase));
        if (probeCandidate is null)
            throw new InvalidOperationException("maximized probe was not enumerated as a surface candidate");
        if (!(bool)Property(probeCandidate, "IsMaximized"))
            throw new InvalidOperationException("maximized probe was not marked as maximized");
        Console.WriteLine("MAXIMIZED WINDOW SURFACE=REJECTED PASS");

    }

    refreshWindowSurfaces.Invoke(host, Array.Empty<object>());
    var displayAreas = displayValues.Select(display =>
    {
        var area = Property(display, "WorkingArea");
        return new
        {
            Area = area,
            Left = Convert.ToSingle(Property(area, "Left")),
            Right = Convert.ToSingle(Property(area, "Right")),
            Top = Convert.ToSingle(Property(area, "Top")),
            Bottom = Convert.ToSingle(Property(area, "Bottom"))
        };
    }).ToArray();
    var horizontalPair = displayAreas
        .SelectMany(source => displayAreas
            .Where(target => target.Left > source.Right)
            .Select(target => new { Source = source, Target = target }))
        .OrderBy(pair => pair.Target.Left - pair.Source.Right)
        .FirstOrDefault();
    if (horizontalPair is null)
    {
        Console.WriteLine("CROSS-DISPLAY WALK=SKIP NO_HORIZONTALLY_ORDERED_DISPLAY_PAIR");
    }
    else
    {
        var createAt = session.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Single(method => method.Name == "Create" && method.GetParameters().Length == 2);
        var crossPet = createAt.Invoke(session, new object[]
        {
            "cat",
            new Vector2(horizontalPair.Source.Right - 1, horizontalPair.Source.Bottom)
        })!;
        var startWalking = crossPet.GetType().GetMethod("StartWalking")!;
        var direction = Enum.Parse(startWalking.GetParameters()[0].ParameterType, "Right");
        startWalking.Invoke(crossPet, new[] { direction });
        var reachedTarget = false;
        for (var tick = 0; tick < 2000; tick++)
        {
            session.GetType().GetMethod("Tick")!.Invoke(session, new object[] { 0.1d });
            var position = Property(crossPet, "FootPosition");
            var x = (float)position.GetType().GetField("X")!.GetValue(position)!;
            if (x >= horizontalPair.Target.Left && x <= horizontalPair.Target.Right)
            {
                reachedTarget = true;
                break;
            }
        }
        var crossInstanceId = (Guid)Property(crossPet, "InstanceId");
        session.GetType().GetMethod("Remove")!.Invoke(session, new object[] { crossInstanceId });
        if (!reachedTarget)
            throw new InvalidOperationException("pet did not walk from the first display into the second display");
        Console.WriteLine($"CROSS-DISPLAY WALK SOURCE_RIGHT={horizontalPair.Source.Right:0} TARGET_LEFT={horizontalPair.Target.Left:0} PASS");

        var surface = (IWorldSurface)session.GetType().GetProperty("Surface")!.GetValue(session)!;
        var tossStart = new Vector2(horizontalPair.Target.Right - 1, horizontalPair.Target.Bottom - 200);
        var tossPet = (object)createAt.Invoke(session, new object[] { "cat", tossStart })!;
        var tossEngine = new PetEngine(surface, new FixedRandom());
        tossEngine.BeginDrag((YimyWalk.Core.Simulation.PetInstance)tossPet);
        tossEngine.ReleaseDrag((YimyWalk.Core.Simulation.PetInstance)tossPet, new Vector2(PetEngine.MaxTossSpeed, 0));
        var wrapped = false;
        var landed = false;
        for (var tick = 0; tick < 200; tick++)
        {
            tossEngine.Tick((YimyWalk.Core.Simulation.PetInstance)tossPet, 0.1);
            var position = (Vector2)Property(tossPet, "FootPosition");
            wrapped |= position.X < horizontalPair.Target.Left;
            landed |= string.Equals(Property(tossPet, "State").ToString(), "Idle", StringComparison.Ordinal);
            if (wrapped && landed) break;
        }
        var tossInstanceId = (Guid)Property(tossPet, "InstanceId");
        session.GetType().GetMethod("Remove")!.Invoke(session, new object[] { tossInstanceId });
        if (!wrapped || !landed)
            throw new InvalidOperationException($"airborne toss did not wrap and land: wrapped={wrapped} landed={landed}");
        Console.WriteLine($"CROSS-DISPLAY TOSS SOURCE_RIGHT={horizontalPair.Target.Right:0} REENTRY_LEFT_OF={horizontalPair.Target.Left:0} PASS");
    }
}

if (landingRequested)
{
    using var landingProbe = new Form
    {
        FormBorderStyle = FormBorderStyle.FixedToolWindow,
        StartPosition = FormStartPosition.Manual,
        ShowInTaskbar = false,
        Bounds = new Rectangle(1370, 300, 120, 240),
        BackColor = Color.CornflowerBlue,
        Text = "YimyWalk landing probe"
    };
    landingProbe.Show();
    Application.DoEvents();
    type.GetProperty("WindowLandingEnabled")!.SetValue(host, true);
    refreshWindowSurfaces.Invoke(host, Array.Empty<object>());
    var createAt = session.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)
        .Single(method => method.Name == "Create" && method.GetParameters().Length == 2);
    createAt.Invoke(session, new object[] { "cat", new Vector2(1430, -32) });
    var landingDeadline = Environment.TickCount64 + 5000;
    while (Environment.TickCount64 < landingDeadline)
    {
        Application.DoEvents();
        Thread.Sleep(15);
        var landingWindows = ((System.Collections.IEnumerable)windows.GetType().GetProperty("Windows")!.GetValue(windows)!).Cast<object>().ToArray();
        if (landingWindows.Length == 1 && string.Equals(Property(Property(landingWindows[0], "Instance"), "State").ToString(), "Idle", StringComparison.Ordinal))
            break;
    }
    var landingPet = ((System.Collections.IEnumerable)windows.GetType().GetProperty("Windows")!.GetValue(windows)!).Cast<object>().Single();
    var landingInstance = Property(landingPet, "Instance");
    var landingY = FootY(landingInstance);
    if (Math.Abs(landingY - landingProbe.Top) > 2)
        throw new InvalidOperationException($"window landing did not select probe top: pet={landingY}, probe={landingProbe.Top}");
    Console.WriteLine($"WINDOW LANDING TOP={landingProbe.Top} PET={landingY} PASS");
    landingProbe.Close();
    refreshWindowSurfaces.Invoke(host, Array.Empty<object>());
    var fallDeadline = Environment.TickCount64 + 5000;
    while (Environment.TickCount64 < fallDeadline && FootY(landingInstance) < landingProbe.Top + 20)
    {
        Application.DoEvents();
        Thread.Sleep(15);
    }
    if (FootY(landingInstance) < landingProbe.Top + 20)
        throw new InvalidOperationException("pet did not re-fall after landing probe closed");
    Console.WriteLine($"WINDOW LANDING CLOSE_REFALL_Y={FootY(landingInstance):0.0} PASS");
    type.GetMethod("ClearPets")!.Invoke(host, Array.Empty<object>());
    type.GetProperty("WindowLandingEnabled")!.SetValue(host, false);
}

var characterRoot = (string)type.GetProperty("CharacterRoot")!.GetValue(host)!;
var catDirectory = Path.Combine(characterRoot, "cat");
var spritePath = Path.Combine(catDirectory, "sprite.png");
var configPath = Path.Combine(catDirectory, "character.json");
var originalSprite = File.ReadAllBytes(spritePath);
var hadConfig = File.Exists(configPath);
var originalConfig = hadConfig ? File.ReadAllBytes(configPath) : Array.Empty<byte>();
try
{
    type.GetMethod("CreateMany")!.Invoke(host, new object[] { "cat", 1 });
    for (var i = 0; i < 10; i++) { Application.DoEvents(); Thread.Sleep(15); }
    var oldPet = ((System.Collections.IEnumerable)windows.GetType().GetProperty("Windows")!.GetValue(windows)!).Cast<object>().Single();
    var oldDefinition = oldPet.GetType().GetProperty("Instance")!.GetValue(oldPet)!.GetType().GetProperty("Definition")!.GetValue(oldPet.GetType().GetProperty("Instance")!.GetValue(oldPet)!);
    File.WriteAllText(configPath, "{\"schemaVersion\":1,\"displayName\":\"Reloaded Cat\"}");
    var reloadMethod = type.GetMethod("ReloadCharacters") ?? throw new MissingMethodException(type.FullName, "ReloadCharacters");
    var validFailures = (System.Collections.IEnumerable)reloadMethod.Invoke(host, Array.Empty<object>())!;
    if (validFailures.Cast<object>().Any()) throw new InvalidOperationException("valid role reload reported failures");
    type.GetMethod("CreateMany")!.Invoke(host, new object[] { "cat", 1 });
    for (var i = 0; i < 10; i++) { Application.DoEvents(); Thread.Sleep(15); }
    var pets = ((System.Collections.IEnumerable)windows.GetType().GetProperty("Windows")!.GetValue(windows)!).Cast<object>().ToArray();
    var newPet = pets.Single(candidate => !ReferenceEquals(candidate, oldPet));
    var newInstance = newPet.GetType().GetProperty("Instance")!.GetValue(newPet)!;
    var newDefinition = newInstance.GetType().GetProperty("Definition")!.GetValue(newInstance);
    if (ReferenceEquals(oldDefinition, newDefinition)) throw new InvalidOperationException("new pet retained old definition");
    File.WriteAllBytes(spritePath, new byte[] { 1, 2, 3 });
    var invalidFailures = (System.Collections.IEnumerable)reloadMethod.Invoke(host, Array.Empty<object>())!;
    if (!invalidFailures.Cast<object>().Any()) throw new InvalidOperationException("invalid role reload did not report a failure");
    var catalog = type.GetProperty("Catalog")!.GetValue(host)!;
    var definitions = catalog.GetType().GetProperty("Definitions")!.GetValue(catalog)!;
    var retained = definitions.GetType().GetProperty("Item")!.GetValue(definitions, new object[] { "cat" });
    if (!ReferenceEquals(newDefinition, retained)) throw new InvalidOperationException("failed reload replaced last good definition");
    Console.WriteLine("ROLE RELOAD SUCCESS_AND_FAILURE_RETENTION=PASS");
}
finally
{
    File.WriteAllBytes(spritePath, originalSprite);
    if (hadConfig) File.WriteAllBytes(configPath, originalConfig);
    else if (File.Exists(configPath)) File.Delete(configPath);
    type.GetMethod("ClearPets")!.Invoke(host, Array.Empty<object>());
    type.GetMethod("ReloadCharacters")?.Invoke(host, Array.Empty<object>());
}

for (var round = 1; round <= rounds; round++)
foreach (var n in new[] { 1, 10, 50, 100 })
{
    type.GetMethod("CreateMany")!.Invoke(host, new object[] { role, n });
    for (var i = 0; i < 10; i++) { Application.DoEvents(); Thread.Sleep(15); }
    using (var process = System.Diagnostics.Process.GetCurrentProcess())
    {
        process.Refresh();
        Console.WriteLine($"ROUND {round} CREATE {n} ACTIVE={active.GetValue(session)} WINDOWS={windowCount.GetValue(windows)} {Metrics(process)}");
    }
    type.GetMethod("ClearPets")!.Invoke(host, Array.Empty<object>());
    for (var i = 0; i < 5; i++) { Application.DoEvents(); Thread.Sleep(15); }
    GC.Collect();
    GC.WaitForPendingFinalizers();
    GC.Collect();
    if ((int)cachedSurfaceCount.GetValue(host)! != 0)
        throw new InvalidOperationException("clear did not release cached rendering surfaces");
    using (var process = System.Diagnostics.Process.GetCurrentProcess())
    {
        process.Refresh();
        Console.WriteLine($"ROUND {round} CLEAR ACTIVE=0 WINDOWS=0 CACHE=0 {Metrics(process)}");
    }
}
if (host is IDisposable disposableHost)
{
    disposableHost.Dispose();
    GC.Collect();
    GC.WaitForPendingFinalizers();
    GC.Collect();
    using var process = System.Diagnostics.Process.GetCurrentProcess();
    process.Refresh();
    Console.WriteLine($"FINAL {Metrics(process)}");
}
if (Directory.Exists(characterTemp)) Directory.Delete(characterTemp, true);

static string Metrics(System.Diagnostics.Process process)
    => $"CPU_S={process.TotalProcessorTime.TotalSeconds:0.000} WS={process.WorkingSet64} PRIVATE={process.PrivateMemorySize64} MANAGED={GC.GetTotalMemory(false)} HANDLES={process.HandleCount} USER={Native.GetGuiResources(process.Handle, 1)} GDI={Native.GetGuiResources(process.Handle, 0)}";

static object Property(object target, string name)
    => target.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(target)!;

static float FootY(object instance)
{
    var value = Property(instance, "FootPosition");
    return (float)value.GetType().GetField("Y")!.GetValue(value)!;
}

static void CopyCharacterFixture(string role, string destinationRoot)
{
    var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../../"));
    var source = role.Equals("cat", StringComparison.OrdinalIgnoreCase)
        ? Path.Combine(repoRoot, "tests", "Fixtures", "Characters", role)
        : Path.Combine(repoRoot, "Characters", role);
    var destination = Path.Combine(destinationRoot, role);
    Directory.CreateDirectory(destination);
    foreach (var file in Directory.EnumerateFiles(source))
        File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), overwrite: true);
}

static bool SameIcon(Icon expected, Icon actual)
{
    using var expectedBitmap = expected.ToBitmap();
    using var actualBitmap = actual.ToBitmap();
    if (expectedBitmap.Size != actualBitmap.Size) return false;
    for (var y = 0; y < expectedBitmap.Height; y++)
        for (var x = 0; x < expectedBitmap.Width; x++)
            if (expectedBitmap.GetPixel(x, y).ToArgb() != actualBitmap.GetPixel(x, y).ToArgb()) return false;
    return true;
}

static class Native
{
    [DllImport("user32.dll")]
    public static extern uint GetGuiResources(IntPtr process, uint flags);
}

sealed class FixedRandom : IRandomSource
{
    public double NextDouble() => 0.5;
    public int Next(int minInclusive, int maxExclusive) => minInclusive;
}
