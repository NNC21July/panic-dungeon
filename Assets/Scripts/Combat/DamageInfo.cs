using UnityEngine;

public enum DamageType
{
    Normal, Spike, Arrow, Poison, Zombie, Melee
}

public readonly struct DamageInfo
{
    public float Amount { get; }
    public GameObject Source { get; }
    public DamageType Type { get; }

    public DamageInfo(float amount, GameObject source, DamageType type)
    {
        Amount = amount;
        Source = source;
        Type = type;
    }
}
