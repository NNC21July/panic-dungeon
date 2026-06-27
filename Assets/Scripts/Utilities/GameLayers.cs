using UnityEngine;

public static class GameLayers
{
    public static readonly int Player = LayerMask.NameToLayer("Player");
    public static readonly int Wall = LayerMask.NameToLayer("Wall");
    public static readonly int Obstacle = LayerMask.NameToLayer("Obstacle");
    public static readonly int Enemy = LayerMask.NameToLayer("Enemy");
}