using Fusion;
using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(NetworkHealth))]
public class NetworkPlayerCombat : NetworkBehaviour
{
    [SerializeField, Min(0f)] private float attackCooldown = 0.45f;
    [SerializeField] private Transform swordPivot;
    [SerializeField] private TrailRenderer swordTrail;
    [SerializeField, Min(0f)] private float swingAngle = 120f;
    [SerializeField, Min(0.01f)] private float swingDuration = 0.3f;
    [SerializeField] private AnimationCurve swingCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private SpriteRenderer swordRenderer;
    [SerializeField, Min(0f)] private float damage = 35f;
    [SerializeField] private BoxCollider2D swordHitbox;
    [SerializeField] private LayerMask damageableLayers;
    [Networked] private bool IsAttacking { get; set; }
    [Networked] private float AttackTimer { get; set; }
    [Networked] private float CooldownTimer { get; set; }
    [Networked] private Vector2 attackDirection { get; set; } = Vector2.right;
    private bool renderedAttackLastFrame;
    private readonly Collider2D[] hitResults = new Collider2D[8];
    private readonly HashSet<NetworkHealth> damagedTargets = new HashSet<NetworkHealth>();
    private ContactFilter2D hitFilter;
    private NetworkHealth networkHealth;

    private void Awake()
    {
        SerializedFieldValidator.Validate(this);

        hitFilter = new ContactFilter2D();
        hitFilter.SetLayerMask(damageableLayers);
        hitFilter.useTriggers = true;
        networkHealth = GetComponent<NetworkHealth>();
    }

    public override void FixedUpdateNetwork()
    {
        if (networkHealth.IsDead)
        {
            IsAttacking = false;
            AttackTimer = 0f;
            return;
        }

        if (!GetInput(out NetworkInputData inputData))
            return;

        if (inputData.Buttons.IsSet(NetworkInputButton.Attack) && CooldownTimer <= 0f && !IsAttacking)
        {
            if (inputData.AimDirection.sqrMagnitude > 0.0001f)
                attackDirection = inputData.AimDirection.normalized;
            IsAttacking = true;
            AttackTimer = swingDuration;
            CooldownTimer = attackCooldown;
            if (Object.HasStateAuthority)
                damagedTargets.Clear();
        }

        if (IsAttacking)
        {
            AttackTimer -= Runner.DeltaTime;
            if (AttackTimer <= 0f)
                IsAttacking = false;
            float currentAngle = GetCurrentSwingAngle();
            swordPivot.rotation = Quaternion.Euler(0f, 0f, currentAngle);
            if (Object.HasStateAuthority)
                CheckForSwordHits();
        }
        if (CooldownTimer > 0f)
            CooldownTimer -= Runner.DeltaTime;
    }

    public override void Render()
    {
        if (!IsAttacking)
        {
            if (swordRenderer != null)
                swordRenderer.enabled = false;
            if (swordTrail != null)
                swordTrail.emitting = false;
            renderedAttackLastFrame = false;
            return;
        }
        if (!renderedAttackLastFrame && swordTrail != null)
            swordTrail.Clear();

        swordPivot.rotation = Quaternion.Euler(0f, 0f, GetCurrentSwingAngle());

        if (swordRenderer != null)
            swordRenderer.enabled = true;
        if (swordTrail != null)
            swordTrail.emitting = true;
        renderedAttackLastFrame = true;
    }

    private void CheckForSwordHits()
    {
        Vector2 center = swordHitbox.transform.TransformPoint(swordHitbox.offset);
        Vector3 scale = swordHitbox.transform.lossyScale;
        Vector2 size = new Vector2(swordHitbox.size.x * Mathf.Abs(scale.x), swordHitbox.size.y * Mathf.Abs(scale.y));
        float angle = swordHitbox.transform.eulerAngles.z;

        PhysicsScene2D physicsScene = Runner.GetPhysicsScene2D();
        int hitCount = physicsScene.OverlapBox(
            center,
            size,
            angle,
            hitFilter,
            hitResults
        );

        for (int i = 0; i < hitCount; i++)
        {
            NetworkHealth targetHealth = hitResults[i].GetComponentInParent<NetworkHealth>();
            if (targetHealth == null)
                continue;
            if (targetHealth.Object == Object)
                continue;
            if (!damagedTargets.Add(targetHealth))
                continue;
            targetHealth.TryTakeDamage(damage);
        }
    }

    private float GetCurrentSwingAngle()
    {
        float progress = 1f - Mathf.Clamp01(AttackTimer / swingDuration),
            curvedProgress = Mathf.Clamp01(swingCurve.Evaluate(progress)),
            baseAngle = Mathf.Atan2(attackDirection.y, attackDirection.x) * Mathf.Rad2Deg,
            startAngle = baseAngle - swingAngle / 2f,
            endAngle = baseAngle + swingAngle / 2f,
            currentAngle = Mathf.Lerp(startAngle, endAngle, curvedProgress);

        return currentAngle;
    }
}