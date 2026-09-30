using Fusion;
using UnityEngine;

// Local playback only. This never approves damage or changes replicated spike state.
public class SpikeObserverHold
{
    public bool IsActive => preview.IsActive;
    public Vector2 Position { get; private set; }
    public int Cycle => preview.Cycle;
    public NetworkId Target => preview.Target;
    private readonly TrapContactPreview<NetworkId> preview = new();
    private int impactCount;

    public bool TryBegin(NetworkSpikeState state, Vector2 position, double now, double duration, NetworkId target = default)
    {
        if (state.Phase != SpikePhase.Advancing || !preview.TryBegin(state.PatternSequence, target, now, duration))
            return false;
        impactCount = state.ImpactCount;
        Position = position;
        return true;
    }

    public Vector2 Evaluate(NetworkSpikeState state, double now, bool safeToHold)
    {
        preview.Evaluate(state.PatternSequence, now, safeToHold && state.ImpactCount == impactCount &&
            (state.Phase == SpikePhase.Advancing || state.Phase == SpikePhase.Holding));
        return IsActive ? Position : state.Position;
    }

    public void Reject(int cycle, NetworkId target)
    {
        preview.Reject(cycle, target);
    }

    public void Release()
    {
        preview.Release();
    }
}
