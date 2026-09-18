using Fusion;
using UnityEngine;

public struct PlayerContactSample : INetworkStruct
{
    public Vector2 Center;
    public double Time;
    public int MovementSequence, CorrectionCount;
}
