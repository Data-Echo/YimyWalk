using YimyWalk.Core.Characters;

namespace YimyWalk.Core.Resources;

/// <summary>
/// Keeps a decoded role definition alive while instances or platform renderer
/// surfaces still refer to it. The owner can be retired during a catalog swap;
/// the final handle then performs the one disposal.
/// </summary>
public sealed class DefinitionLease : IDisposable
{
    private readonly object _gate = new();
    private bool _ownerDisposed;
    private bool _released;
    private int _instanceCount;
    private int _rendererCount;

    public DefinitionLease(CharacterDefinition definition)
        => Definition = definition ?? throw new ArgumentNullException(nameof(definition));

    public CharacterDefinition Definition { get; }
    public int ActiveInstanceCount { get { lock (_gate) return _instanceCount; } }
    public int ActiveRendererCount { get { lock (_gate) return _rendererCount; } }
    public bool IsReleased { get { lock (_gate) return _released; } }

    public LeaseHandle AcquireInstance() => Acquire(LeaseKind.Instance);
    public LeaseHandle AcquireRenderer() => Acquire(LeaseKind.Renderer);

    public void Dispose()
    {
        lock (_gate)
        {
            _ownerDisposed = true;
            TryReleaseDefinition();
        }
    }

    private LeaseHandle Acquire(LeaseKind kind)
    {
        lock (_gate)
        {
            if (_ownerDisposed || _released)
                throw new ObjectDisposedException(nameof(DefinitionLease));
            if (kind == LeaseKind.Instance) _instanceCount++;
            else _rendererCount++;
            return new LeaseHandle(this, kind);
        }
    }

    private void Release(LeaseKind kind)
    {
        lock (_gate)
        {
            if (kind == LeaseKind.Instance && _instanceCount > 0) _instanceCount--;
            if (kind == LeaseKind.Renderer && _rendererCount > 0) _rendererCount--;
            TryReleaseDefinition();
        }
    }

    private void TryReleaseDefinition()
    {
        if (_ownerDisposed && !_released && _instanceCount == 0 && _rendererCount == 0)
        {
            _released = true;
            Definition.Dispose();
        }
    }

    internal enum LeaseKind { Instance, Renderer }

    public sealed class LeaseHandle : IDisposable
    {
        private DefinitionLease? _owner;
        private readonly LeaseKind _kind;

        internal LeaseHandle(DefinitionLease owner, LeaseKind kind)
        {
            _owner = owner;
            _kind = kind;
        }

        public CharacterDefinition Definition => _owner?.Definition ?? throw new ObjectDisposedException(nameof(LeaseHandle));

        public void Dispose()
        {
            var owner = Interlocked.Exchange(ref _owner, null);
            owner?.Release(_kind);
        }
    }
}
