using UnityEngine;

[CreateAssetMenu(fileName = "ZombieTuning", menuName = "Scriptable Objects/ZombieTuning")]
public class ZombieTuning : ScriptableObject
{
    [SerializeField, Min(0f)] private float moveSpeed = 2f, attackRange = 0.8f, attackDamage = 15f, attackCooldown = 1f;
    [SerializeField, Min(0f)] private float targetSwitchMargin = 0.3f;

    public float MoveSpeed => moveSpeed;
    public float AttackRange => attackRange;
    public float AttackDamage => attackDamage;
    public float AttackCooldown => attackCooldown;
    public float TargetSwitchMargin => targetSwitchMargin;
}
