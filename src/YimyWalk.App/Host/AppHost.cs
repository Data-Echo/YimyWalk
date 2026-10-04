using YimyWalk.App.Rendering;
using YimyWalk.App.Settings;
using YimyWalk.App.UI;
using YimyWalk.App.Windows;
using YimyWalk.Core.Characters;
using YimyWalk.Core.Display;
using YimyWalk.Core.Settings;
using YimyWalk.Core.Simulation;
using System.Diagnostics;
using System.Numerics;

namespace YimyWalk.App.Host;

internal sealed class AppHost : ApplicationContext
{
    private readonly DesktopWorldSurface _surface = new();
    private readonly PetWindowRegistry _windows = new();
    private readonly FrameSurfaceCache _surfaceCache = new();
    private readonly CharacterCatalog _catalog = new();
    private readonly SettingsStore _settingsStore = new();
    private readonly PetSession _session;
    private readonly System.Windows.Forms.Timer _timer;
    private readonly BatchCreationQueue _batch;
    private readonly ManagerForm _manager;
    private readonly TrayIconHost _tray;
    private readonly DialogueScheduler _dialogue = new();
    private readonly Dictionary<Guid, BubbleWindow> _bubbleWindows = new();
    private readonly VirtualFoodSystem _virtualFood = new();
    private readonly Dictionary<Guid, FoodWindow> _foodWindows = new();
    private readonly HashSet<string> _registeredDefinitionIds = new(StringComparer.OrdinalIgnoreCase);
    private SettingsDocument _settings = SettingsDocument.Defaults;
    private SettingsLoadResult _settingsLoad = new(SettingsDocument.Defaults, UsedDefaults: true, UsedBackup: false, null);
    private bool _disposed;
    private long _lastTickTimestamp;
    private double _dialogueClock;
    internal bool IsExiting { get; private set; }

    public AppHost()
    {
        _session = new PetSession(_surface, new SystemRandomSource());
        _session.InstanceAdded += OnInstanceAdded;
        _session.InstanceRemoved += OnInstanceRemoved;
        _timer = new System.Windows.Forms.Timer { Interval = 16 };
        _timer.Tick += (_, _) => Tick();
        MainForm = new Form { ShowInTaskbar = false, Opacity = 0, Width = 1, Height = 1 };
        MainForm.Load += (_, _) => MainForm.Hide();
        _batch = new BatchCreationQueue(() =>
        {
            if (!_catalog.TryGet(_settings.SelectedRoleId, out var definition))
                definition = _catalog.Definitions.Values.FirstOrDefault()
                    ?? throw new InvalidOperationException("no character role is loaded");
            return _session.Create(definition.Id);
        });
        _manager = new ManagerForm(this);
        _tray = new TrayIconHost(this);
    }

    public PetSession Session => _session;
    public PetWindowRegistry Windows => _windows;
    public CharacterCatalog Catalog => _catalog;
    public string CharacterRoot { get; private set; } = string.Empty;
    public BatchCreationQueue Batch => _batch;
    public SettingsDocument Settings => _settings;
    public SettingsLoadResult SettingsLoad => _settingsLoad;
    public string SelectedRoleId => _settings.SelectedRoleId;
    public float GlobalScale => _settings.Scale;
    public IDisplayProvider DisplayProvider => _surface.DisplayProvider;
    public IReadOnlyList<DisplayInfo> Displays => _surface.Snapshot.Displays;
    public bool WindowLandingEnabled
    {
        get => _surface.WindowLandingEnabled;
        set
        {
            if (_surface.WindowLandingEnabled == value) return;
            _surface.WindowLandingEnabled = value;
            if (!_disposed && CharacterRoot.Length > 0)
            {
                _settings = _settings with { WindowLandingEnabled = value };
                SaveSettings();
            }
        }
    }
    public bool HungerEnabled
    {
        get => _session.HungerEnabled;
        set
        {
            if (_session.HungerEnabled == value) return;
            _session.HungerEnabled = value;
            if (!_disposed && CharacterRoot.Length > 0)
            {
                _settings = _settings with { HungerEnabled = value };
                SaveSettings();
            }
        }
    }
    public bool WindowSurfaceSnapshotValid => _surface.WindowSurfaceSnapshotValid;
    public IReadOnlyList<WindowSurfaceCandidate> WindowSurfaceCandidates => _surface.WindowSurfaceCandidates;
    public int CachedSurfaceCount => _surfaceCache.Count;
    public int DialogueBubbleCount => _dialogue.Active.Count;
    public int VirtualFoodCount => _virtualFood.Targets.Count;
    public bool DialogueEnabled
    {
        get => _settings.DialogueEnabled;
        set
        {
            if (_settings.DialogueEnabled == value) return;
            _settings = _settings with { DialogueEnabled = value };
            if (!value) ClearDialogues();
            if (!_disposed && CharacterRoot.Length > 0) SaveSettings();
        }
    }

    public void RefreshWindowSurfaces() => _surface.RefreshWindowSurfaces();

    public void Start()
    {
        _surface.ExcludeWindow = IsHostWindow;
        if (MainForm is { } form)
        _surface.RefreshForWindow(form.Handle);
        else
            _surface.RefreshNow();
        _surface.RefreshWindowSurfaces();
        _settingsLoad = _settingsStore.Load();
        _settings = _settingsLoad.Settings;
        _surface.SetPreferredDisplay(_settings.PreferredDisplayId);
        _surface.WindowLandingEnabled = _settings.WindowLandingEnabled;
        _session.HungerEnabled = _settings.HungerEnabled;
        _session.NeedRate = _settings.HungerRate;
        var root = Path.Combine(AppContext.BaseDirectory, "Characters");
        if (!Directory.Exists(root)) root = Path.Combine(Environment.CurrentDirectory, "Characters");
        CharacterRoot = root;
        var definitions = _catalog.Scan(root);
        ApplyDefinitions(definitions);
        _timer.Start();
        _lastTickTimestamp = Stopwatch.GetTimestamp();
        _dialogueClock = 0;
        _manager.RefreshCharacters();
        _manager.SetReloadStatus(definitions.Count, _catalog.LastFailures.Count);
        _manager.SetSettingsStatus(GetSettingsStatus());
    }

    public IReadOnlyList<CharacterScanFailure> ReloadCharacters()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (string.IsNullOrWhiteSpace(CharacterRoot))
            throw new InvalidOperationException("The host has not been started.");

        var definitions = _catalog.Scan(CharacterRoot);
        ApplyDefinitions(definitions);
        _manager.RefreshCharacters();
        _manager.SetReloadStatus(definitions.Count, _catalog.LastFailures.Count);
        return _catalog.LastFailures;
    }

    public void CreateMany(string id, int count)
    {
        _session.CreateMany(_catalog.TryGet(id, out var definition) ? definition : throw new KeyNotFoundException(id), count);
    }

    public void ClearPets()
    {
        _batch.Cancel();
        _session.Clear();
        ClearVirtualFood();
    }

    private void ClearVirtualFood()
    {
        _virtualFood.Clear();
        foreach (var foodWindow in _foodWindows.Values) foodWindow.Dispose();
        _foodWindows.Clear();
    }

    public int FeedAll()
    {
        var fed = _session.FeedAll();
        foreach (var instance in _session.Instances.Where(instance => instance.State == PetState.Eat))
            PublishDialogue(instance, "ate");
        return fed;
    }

    public void SpawnVirtualFood()
    {
        var area = _surface.SpawnArea;
        _virtualFood.Spawn(new System.Numerics.Vector2(area.Left + area.Width * 0.5f, area.Bottom));
    }

    public void QueueCreate(string id, int count)
    {
        if (!_catalog.TryGet(id, out var definition)) return;
        _batch.Enqueue(() => _session.Create(definition.Id), count);
    }

    public void SetSelectedRole(string id)
    {
        if (string.IsNullOrWhiteSpace(id) || !_catalog.TryGet(id, out _)) return;
        if (_settings.SelectedRoleId.Equals(id, StringComparison.OrdinalIgnoreCase)) return;
        _settings = _settings with { SelectedRoleId = id };
        SaveSettings();
    }

    public void ShowManager()
    {
        _manager.Show();
        _manager.BringToFront();
    }

    public void SetPetsVisible(bool visible)
    {
        foreach (var window in _windows.Windows)
            window.SetVisibleWithoutActivation(visible);
    }

    public void ExitApplication()
    {
        IsExiting = true;
        _batch.Cancel();
        ExitThread();
    }

    private void Tick()
    {
        var now = Stopwatch.GetTimestamp();
        var delta = _lastTickTimestamp == 0
            ? _timer.Interval / 1000d
            : (now - _lastTickTimestamp) / (double)Stopwatch.Frequency;
        _lastTickTimestamp = now;
        _session.Tick(Math.Clamp(delta, 0, PetEngine.MaxDeltaSeconds));
        foreach (var target in _virtualFood.Targets.Where(target => target.State == VirtualFoodState.Available).ToArray())
        {
            var pet = _session.Instances
                .Where(pet => pet.State is PetState.Idle or PetState.Walk)
                .OrderBy(pet => Vector2.DistanceSquared(pet.FootPosition, target.Position))
                .FirstOrDefault();
            if (pet is not null) _virtualFood.TryClaim(pet, target.Id);
        }
        _virtualFood.Tick(_session.Instances, delta);
        var activeFoodIds = _virtualFood.Targets.Select(target => target.Id).ToHashSet();
        foreach (var stale in _foodWindows.Keys.Where(id => !activeFoodIds.Contains(id)).ToArray())
        {
            _foodWindows[stale].Dispose();
            _foodWindows.Remove(stale);
        }
        foreach (var target in _virtualFood.Targets)
        {
            if (!_foodWindows.TryGetValue(target.Id, out var foodWindow))
            {
                foodWindow = new FoodWindow(Point.Empty);
                _foodWindows[target.Id] = foodWindow;
                foodWindow.Show();
            }
            foodWindow.UpdateTarget(
                new Point((int)Math.Round(target.Position.X - 10), (int)Math.Round(target.Position.Y - 10)),
                target.VisibleScale);
        }
        _dialogueClock += Math.Clamp(delta, 0, PetEngine.MaxDeltaSeconds);
        _dialogue.Advance(_dialogueClock);
        foreach (var expired in _bubbleWindows.Keys.Where(id => !_dialogue.Active.Any(bubble => bubble.Id == id)).ToArray())
        {
            _bubbleWindows[expired].Dispose();
            _bubbleWindows.Remove(expired);
        }
        foreach (var window in _windows.Windows) window.RefreshFrame();
        foreach (var bubble in _dialogue.Active)
            if (_bubbleWindows.TryGetValue(bubble.Id, out var bubbleWindow)
                && _windows.TryGet(bubble.PetId, out var petWindow) && petWindow is not null)
                bubbleWindow.MoveAbove(petWindow.Bounds);
    }

    private void OnInstanceAdded(PetInstance instance)
    {
        var window = new PetWindow(instance, _session, _surfaceCache, _settings.Scale);
        window.Interaction += group => PublishDialogue(instance, group);
        _windows.Add(window);
        PublishDialogue(instance, "created");
    }

    private void OnInstanceRemoved(PetInstance instance)
    {
        _windows.Remove(instance.InstanceId);
        _dialogue.RemovePet(instance.InstanceId);
        foreach (var bubbleId in _bubbleWindows.Where(pair => pair.Value is not null && _dialogue.Active.All(bubble => bubble.Id != pair.Key)).Select(pair => pair.Key).ToArray())
        {
            _bubbleWindows[bubbleId].Dispose();
            _bubbleWindows.Remove(bubbleId);
        }
        _batch.NotifyRemoved();
    }

    private void PublishDialogue(PetInstance instance, string group)
    {
        if (!DialogueEnabled) return;
        var result = _dialogue.TryPublish(instance, group, _dialogueClock);
        if (!result.Accepted || result.Bubble is not { } bubble) return;
        if (!_windows.TryGet(instance.InstanceId, out var petWindow) || petWindow is null) return;
        var bubbleWindow = new BubbleWindow(bubble.Text, new Point(petWindow.Bounds.Left, Math.Max(0, petWindow.Bounds.Top - 40)));
        _bubbleWindows[bubble.Id] = bubbleWindow;
        bubbleWindow.Show();
        bubbleWindow.MoveAbove(petWindow.Bounds);
    }

    private void ClearDialogues()
    {
        _dialogue.Clear();
        foreach (var bubble in _bubbleWindows.Values) bubble.Dispose();
        _bubbleWindows.Clear();
    }

    private bool IsHostWindow(IntPtr handle)
    {
        if (MainForm?.IsHandleCreated == true && MainForm.Handle == handle) return true;
        if (_manager.IsHandleCreated && _manager.Handle == handle) return true;
        return _windows.Windows.Any(window => window.IsHandleCreated && window.Handle == handle);
    }

    private void ApplyDefinitions(IReadOnlyDictionary<string, CharacterDefinition> definitions)
    {
        foreach (var definition in definitions.Values)
            _session.ReplaceDefinition(definition.Id, definition);

        foreach (var removedId in _registeredDefinitionIds.Where(id => !definitions.ContainsKey(id)).ToArray())
            _session.RemoveDefinition(removedId);

        _registeredDefinitionIds.Clear();
        foreach (var id in definitions.Keys)
            _registeredDefinitionIds.Add(id);
    }

    private string GetSettingsStatus()
    {
        if (_settingsLoad.UsedBackup) return "Settings: recovered from backup";
        if (_settingsLoad.UsedDefaults)
            return string.IsNullOrWhiteSpace(_settingsLoad.Diagnostic)
                ? "Settings: using defaults"
                : $"Settings: using defaults ({_settingsLoad.Diagnostic})";
        return "Settings: loaded";
    }

    private void SaveSettings()
    {
        try
        {
            _settingsStore.Save(_settings);
            _settingsLoad = new SettingsLoadResult(_settings, UsedDefaults: false, UsedBackup: false, null);
            if (!_disposed) _manager.SetSettingsStatus("Settings: saved");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            if (!_disposed) _manager.SetSettingsStatus($"Settings: save failed ({ex.Message})");
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (_disposed) return;
        _disposed = true;
        if (disposing)
        {
            if (CharacterRoot.Length > 0) SaveSettings();
            _timer.Stop();
            _timer.Dispose();
            _batch.Dispose();
            ClearDialogues();
            ClearVirtualFood();
            _tray.Dispose();
            _manager.Dispose();
            _session.Dispose();
            _catalog.Dispose();
            _registeredDefinitionIds.Clear();
            _windows.Dispose();
            _surfaceCache.Dispose();
            _surface.Dispose();
            MainForm?.Dispose();
        }
        base.Dispose(disposing);
    }
}
