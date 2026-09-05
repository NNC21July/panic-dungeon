using UnityEngine;

[CreateAssetMenu(fileName = "PlayerTuning", menuName = "Scriptable Objects/PlayerTuning")]
public class PlayerTuning : ScriptableObject
{
    [Header("Movement")]
    [SerializeField, Min(0f)] private float moveSpeed = 5f, acceleration = 75f, deceleration = 50f;

    [Header("Dash")]
    [SerializeField, Min(0f)] private float dashSpeed = 22f;
    [SerializeField, Min(0.01f)] private float dashDuration = 0.15f;
    [SerializeField, Min(0f)] private float dashCooldown = 1f;
    [SerializeField] private AnimationCurve dashSpeedCurve = new AnimationCurve(
        new Keyframe(0f, 1.398873f, 0f, 0f),
        new Keyframe(1.0074735f, 0.25000653f, 0f, 0f)
    );

    [Header("Combat")]
    [SerializeField, Min(0f)] private float swingAngle = 120f;
    [SerializeField, Min(0.01f)] private float swingDuration = 0.3f;
    [SerializeField, Min(0f)] private float attackCooldown = 0.45f;
    [SerializeField, Min(0f)] private float swordDamage = 35f;
    [SerializeField] private AnimationCurve swingCurve = new AnimationCurve(
        new Keyframe(0f, 0f, 0f, 0f),
        new Keyframe(0.49997303f, 0.65056086f, 1f, 1f),
        new Keyframe(1f, 1f, 0f, 0f)
    );

    public float MoveSpeed => moveSpeed;
    public float Acceleration => acceleration;
    public float Deceleration => deceleration;
    public float DashSpeed => dashSpeed;
    public float DashDuration => dashDuration;
    public float DashCooldown => dashCooldown;
    public AnimationCurve DashSpeedCurve => dashSpeedCurve;
    public float SwingAngle => swingAngle;
    public float SwingDuration => swingDuration;
    public float AttackCooldown => attackCooldown;
    public float SwordDamage => swordDamage;
    public AnimationCurve SwingCurve => swingCurve;
}
