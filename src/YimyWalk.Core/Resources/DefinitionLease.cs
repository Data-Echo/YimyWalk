using System.Runtime.CompilerServices;
using YimyWalk.Core.Characters;

namespace YimyWalk.Core.Resources;

/// <summary>
/// Keeps a decoded role definition alive while catalog, instances, or platform
/// renderer surfaces still refer to it. Wrappers for the same definition share
/// one lifetime state so replacing one owner cannot dispose another owner's data.
/// </summary>
public sealed class DefinitionLease : IDisposable
{
    private static readonly ConditionalWeakTable<CharacterDefinition, SharedState> States = new();
    private readonly SharedState _state;
    private bool _ownerDisposed;

    public DefinitionLease(CharacterDefinition definition)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        _state = States.GetValue(definition, static _ => new SharedState());
        lock (_state.Gate)
        {
            _state.Definition ??= definition;
            _state.OwnerCount++;
        }
    }

    public CharacterDefinition Definition { get; }
    public int ActiveInstanceCount { get { lock (_state.Gate) return _state.InstanceCount; } }
    public int ActiveRendererCount { get { lock (_state.Gate) return _state.RendererCount; } }
    public bool IsReleased { get { lock (_state.Gate) return _state.Released; } }

    public LeaseHandle AcquireInstance() => Acquire(LeaseKind.Instance);
    public LeaseHandle AcquireRenderer() => Acquire(LeaseKind.Renderer);

    public void Dispose()
    {
        lock (_state.Gate)
        {
            if (_ownerDisposed) return;
            _ownerDisposed = true;
            _state.OwnerCount--;
            TryReleaseDefinition(_state);
        }
    }

    private LeaseHandle Acquire(LeaseKind kind)
    {
        lock (_state.Gate)
        {
            if (_ownerDisposed || _state.Released)
                throw new ObjectDisposedException(nameof(DefinitionLease));
            if (kind == LeaseKind.Instance) _state.InstanceCount++;
            else _state.RendererCount++;
            return new LeaseHandle(_state, kind);
        }
    }

    private static void Release(SharedState state, LeaseKind kind)
    {
        lock (state.Gate)
        {
            if (kind == LeaseKind.Instance && state.InstanceCount > 0) state.InstanceCount--;
            if (kind == LeaseKind.Renderer && state.RendererCount > 0) state.RendererCount--;
            TryReleaseDefinition(state);
        }
    }

    private static void TryReleaseDefinition(SharedState state)
    {
        if (state.OwnerCount == 0 && !state.Released && state.InstanceCount == 0 && state.RendererCount == 0)
        {
            state.Released = true;
            state.Definition?.Dispose();
        }
    }

    internal sealed class SharedState
    {
        public readonly object Gate = new();
        public CharacterDefinition? Definition;
        public int OwnerCount;
        public int InstanceCount;
        public int RendererCount;
        public bool Released;
    }

    internal enum LeaseKind { Instance, Renderer }

    public sealed class LeaseHandle : IDisposable
    {
        private SharedState? _state;
        private readonly LeaseKind _kind;

        internal LeaseHandle(SharedState state, LeaseKind kind)
        {
            _state = state;
            _kind = kind;
        }

        public void Dispose()
        {
            var state = Interlocked.Exchange(ref _state, null);
            if (state is not null) DefinitionLease.Release(state, _kind);
        }
    }
}
