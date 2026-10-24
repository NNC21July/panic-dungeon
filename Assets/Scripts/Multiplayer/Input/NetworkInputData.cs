using Fusion;
using UnityEngine;

public enum NetworkInputButton
{
    Dash,
    Attack
}

public struct NetworkInputData : INetworkInput
{
    public Vector2 MoveDir, AimDirection;
    public NetworkButtons Buttons;
}
