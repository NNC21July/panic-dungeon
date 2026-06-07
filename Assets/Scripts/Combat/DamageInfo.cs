using UnityEngine;

public enum DamageType
{
    Normal, Spike, Arrow, Poison, Zombie
}

public struct DamageInfo
{
    public float Amount;
    public GameObject Source;
    public DamageType Type;

    public DamageInfo(float amount, GameObject source, DamageType type)
    {
        Amount = amount;
        Source = source;
        Type = type;
    }
}
