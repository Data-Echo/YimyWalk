using YimyWalk.App.Windows;
using YimyWalk.Core.Simulation;

namespace YimyWalk.App.Host;

internal sealed class PetWindowRegistry : IDisposable
{
    private readonly Dictionary<Guid, PetWindow> _windows = new();

    public int Count => _windows.Count;
    public IReadOnlyCollection<PetWindow> Windows => _windows.Values.ToArray();

    public bool TryGet(Guid instanceId, out PetWindow? window) => _windows.TryGetValue(instanceId, out window);

    public void Add(PetWindow window)
    {
        if (_windows.TryGetValue(window.Instance.InstanceId, out var previous))
            previous.Dispose();
        _windows[window.Instance.InstanceId] = window;
        window.Show();
        // The handle now exists at its target display; refresh once so the
        // first bitmap uses the target monitor's DPI.
        window.RefreshFrame();
    }

    public bool Remove(Guid instanceId)
    {
        if (!_windows.Remove(instanceId, out var window)) return false;
        window.Dispose();
        return true;
    }

    public void Clear()
    {
        foreach (var window in _windows.Values.ToArray()) window.Dispose();
        _windows.Clear();
    }

    public void Dispose() => Clear();
}
