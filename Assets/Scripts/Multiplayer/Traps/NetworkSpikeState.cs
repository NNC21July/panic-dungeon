using Fusion;
using UnityEngine;

public enum SpikePhase
{
    Idle,
    Warning,
    Advancing,
    Holding,
    Stopped,
    Retracting
}

public struct NetworkSpikeState : INetworkStruct
{
    public Vector2 Position;
    public int PatternSequence, ImpactCount;
    public SpikePhase Phase;
    public NetworkBool IsSolid;
    public float WarningBlend;

    public static bool CanCheckContact(NetworkSpikeState first, NetworkSpikeState last, int cycle)
    {
        return first.PatternSequence == cycle && last.PatternSequence == cycle &&
            first.Phase == SpikePhase.Advancing && last.Phase == SpikePhase.Advancing &&
            first.ImpactCount == last.ImpactCount;
    }

    public static NetworkSpikeState Interpolate(NetworkSpikeState from, NetworkSpikeState to, float alpha)
    {
        if (alpha >= 1f)
            return to;
        // Do not sweep across an idle reset, a new attack, or a confirmed contact stop.
        if (from.PatternSequence != to.PatternSequence || from.Phase != to.Phase || from.ImpactCount != to.ImpactCount)
            return from;
        from.Position = Vector2.Lerp(from.Position, to.Position, alpha);
        from.WarningBlend = Mathf.Lerp(from.WarningBlend, to.WarningBlend, alpha);
        return from;
    }
}
