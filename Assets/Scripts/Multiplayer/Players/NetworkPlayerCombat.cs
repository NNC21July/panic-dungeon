using Fusion;
using NaughtyAttributes;
using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(NetworkHealth))]
[RequireComponent(typeof(NetworkPlayerGameplayGate))]
public class NetworkPlayerCombat : NetworkBehaviour
{
    [SerializeField, AllowNesting, Required] private PlayerTuning playerTuning;
    [SerializeField, AllowNesting, Required] private Transform swordPivot;
    [SerializeField, AllowNesting, Required] private TrailRenderer swordTrail;
    [SerializeField, AllowNesting, Required] private SpriteRenderer swordRenderer;
    [SerializeField, AllowNesting, Required] private BoxCollider2D swordHitbox;
    [SerializeField] private LayerMask damageableLayers;
    [Networked] private bool IsAttacking { get; set; }
    [Networked] private float AttackTimer { get; set; }
    [Networked] private float CooldownTimer { get; set; }
    [Networked] public int SwingTick { get; private set; }
    [Networked] private Vector2 attackDirection { get; set; } = Vector2.right;
    private bool renderedAttackLastFrame;
    private readonly Collider2D[] hitResults = new Collider2D[8];
    private readonly HashSet<NetworkHealth> damagedTargets = new HashSet<NetworkHealth>();
    private ContactFilter2D hitFilter;
    private NetworkPlayerGameplayGate gameplayGate;

    private void Awake()
    {
        SerializedFieldValidator.Validate(this);

        hitFilter = new ContactFilter2D();
        hitFilter.SetLayerMask(damageableLayers);
        hitFilter.useTriggers = true;
        gameplayGate = GetComponent<NetworkPlayerGameplayGate>();
    }

    public override void FixedUpdateNetwork()
    {
        if (!gameplayGate.CanAct)
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
            SwingTick = Runner.Tick.Raw;
            AttackTimer = playerTuning.SwingDuration;
            CooldownTimer = playerTuning.AttackCooldown;
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
        // The creator runs a hidden server and a visible client in the same process.
        // Never re-enable the hidden runner's sword when presenting an attack.
        if (!Runner.GetVisible())
        {
            if (swordRenderer != null)
                swordRenderer.enabled = false;
            if (swordTrail != null)
            {
                swordTrail.emitting = false;
                swordTrail.enabled = false;
                swordTrail.Clear();
            }
            renderedAttackLastFrame = false;
            return;
        }
        if (swordTrail != null)
            swordTrail.enabled = true;
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

    private void LateUpdate()
    {
        if (Object == null || !Object.IsValid || !Object.HasInputAuthority || Object.HasStateAuthority ||
            !Runner.GetVisible() || !IsAttacking || !gameplayGate.CanAct)
            return;
        // Query the final rendered poses, not last physics step's remote zombie positions.
        Physics2D.SyncTransforms();
        CheckForSwordHits(true);
    }

    private void CheckForSwordHits(bool previewOnly = false)
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
            if (previewOnly)
            {
                if (targetHealth.GetComponent<NetworkEnemy>() != null)
                    targetHealth.PreviewSwordContact(Object.Id, SwingTick);
                continue;
            }
            if (!damagedTargets.Add(targetHealth))
                continue;
            targetHealth.TakeSwordDamage(new DamageInfo(playerTuning.SwordDamage, gameObject, DamageType.Melee), Object.Id, SwingTick);
        }
    }

    private float GetCurrentSwingAngle()
    {
        float progress = 1f - AttackTimer / playerTuning.SwingDuration;

        return SwordSwingMotion.CalculateAngle(
            attackDirection,
            progress,
            playerTuning.SwingAngle,
            playerTuning.SwingCurve
        );
    }
}
