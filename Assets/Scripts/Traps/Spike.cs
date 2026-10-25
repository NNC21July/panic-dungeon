using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using NaughtyAttributes;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(PolygonCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class Spike : MonoBehaviour
{
    private static readonly WaitForFixedUpdate FixedUpdateWait = new WaitForFixedUpdate();
    [SerializeField, BoxGroup("Movement and Damage"), Min(0.01f)] private float moveDuration = 5f;
    [SerializeField, BoxGroup("Movement and Damage"), Min(0f)] private float damageAmount = 25f, minLaneOverlap = 0f;
    [SerializeField, BoxGroup("Feedback")] private Color warningFlashColor;
    [SerializeField, BoxGroup("Feedback"), Required] private ParticleSystem impactEffect;
    [SerializeField, BoxGroup("Feedback"), Required] private AudioClip impactSfx;
    [SerializeField, BoxGroup("Body References"), Required] private Collider2D solidTipCollider;
    [SerializeField, BoxGroup("Body References"), Required] private Transform tipPoint;
    [SerializeField, BoxGroup("Body References"), Required] private SpriteRenderer pillarBody;
    private Vector2 originPos, targetPos, blockedAt = new Vector2(0, 0);
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private Coroutine warningCoroutine, moveCoroutine;
    private bool isAttacking = false, isBlocked = false, isCycleActive = false;
    private PolygonCollider2D spikeCollider;
    private Rigidbody2D rb;
    private HashSet<IDamageable> damagedTargets;
    private float blockedPathProgress;
    private bool isWarning, isRetracting;
    private float warningBlend;
    private int impactCount;
    private RigidbodyInterpolation2D originalInterpolation;
    public Vector2 BodyPosition => rb.position;
    public PolygonCollider2D DamageCollider => spikeCollider;
    [ShowNativeProperty]
    public bool IsWarning => isWarning;
    [ShowNativeProperty]
    public bool IsAttacking => isAttacking;
    [ShowNativeProperty]
    public bool IsRetracting => isRetracting;
    [ShowNativeProperty]
    public bool IsCycleActive => isCycleActive;
    [ShowNativeProperty]
    public bool IsBlocked => isBlocked;
    public bool IsSolid => solidTipCollider.enabled;
    public float WarningBlend => warningBlend;
    [ShowNativeProperty]
    public int ImpactCount => impactCount;
    public float MoveDuration => moveDuration;
    public float BlockedPathProgress => blockedPathProgress;
    public Vector2 TipPos => tipPoint.position;
    public Vector2 AttackDir => (targetPos - originPos).normalized;
    public Func<IDamageable, DamageInfo, bool> DamageContactHandler { get; set; }
    public event Action<Spike, float> CollisionStopped;
    public bool ResolveContactsLocally { get; set; } = true;

    private void Awake()
    {
        SerializedFieldValidator.Validate(this);
        spriteRenderer = GetComponent<SpriteRenderer>();
        spikeCollider = GetComponent<PolygonCollider2D>();
        rb = GetComponent<Rigidbody2D>();
        originalInterpolation = rb.interpolation;
        damagedTargets = new HashSet<IDamageable>();
        originalColor = spriteRenderer.color;
        SetDamageActive(false);
        solidTipCollider.enabled = false;
    }

    public void ConfigurePos(Vector2 origin, Vector2 target)
    {
        originPos = origin;
        targetPos = target;
    }

    public void ConfigureBodyLength(float worldLength)
    {
        float parentScaleY = Mathf.Abs(transform.lossyScale.y),
              localLength = worldLength / parentScaleY,
              originalSpriteHeight = pillarBody.sprite.bounds.size.y;
        Vector3 bodyScale = pillarBody.transform.localScale;
        bodyScale.y = localLength / originalSpriteHeight;
        pillarBody.transform.localScale = bodyScale;

        float spikeBaseY = spriteRenderer.sprite.bounds.min.y;
        Vector3 bodyPos = pillarBody.transform.localPosition;
        bodyPos.y = spikeBaseY - localLength / 2f;
        pillarBody.transform.localPosition = bodyPos;
    }

    public void ForceIdle()
    {
        isCycleActive = false;
        isWarning = false;
        isRetracting = false;
        warningBlend = 0f;

        if (warningCoroutine != null)
        {
            StopCoroutine(warningCoroutine);
            warningCoroutine = null;
        }
        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
            moveCoroutine = null;
        }
        SetDamageActive(false);
        rb.MovePosition(originPos);
        spriteRenderer.color = originalColor;
        damagedTargets.Clear();
        isBlocked = false;
        blockedPathProgress = 0f;
        blockedAt = originPos;
        solidTipCollider.enabled = false;
    }

    public void BeginWarning(float newWarningDuration, float newWarningFlashDuration)
    {
        isWarning = true;
        newWarningDuration = Mathf.Max(0.01f, newWarningDuration);
        newWarningFlashDuration = Mathf.Max(0.01f, newWarningFlashDuration);
        SetDamageActive(false);

        if (warningCoroutine != null)
            StopCoroutine(warningCoroutine);
        warningCoroutine = StartCoroutine(WarningFlash(newWarningDuration, newWarningFlashDuration));
    }

    public void BeginAttack()
    {
        isCycleActive = true;
        isWarning = false;
        isRetracting = false;
        warningBlend = 0f;

        if (warningCoroutine != null)
        {
            StopCoroutine(warningCoroutine);
            warningCoroutine = null;
        }

        spriteRenderer.color = originalColor;
        SetDamageActive(true);

        if (moveCoroutine != null)
            StopCoroutine(moveCoroutine);
        moveCoroutine = StartCoroutine(Move(originPos, targetPos, moveDuration, true));
        solidTipCollider.enabled = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!ResolveContactsLocally || !isAttacking || isBlocked)
            return;

        int otherLayer = other.gameObject.layer;

        if (otherLayer == GameLayers.Obstacle) // stops when hitting an obstacle
        {
            if (solidTipCollider.bounds.max.x >= other.bounds.min.x + minLaneOverlap && solidTipCollider.bounds.min.x <= other.bounds.max.x - minLaneOverlap) // check if spike and obstacle overlaps horizontally
                StopFromCollision();
            return;
        }

        if (otherLayer != GameLayers.Player && otherLayer != GameLayers.Enemy)
            return;

        IDamageable damageable = other.GetComponentInParent<IDamageable>();
        if (damageable == null)
            return;

        if (damagedTargets.Contains(damageable))
            return;

        DamageInfo damageInfo = new DamageInfo(damageAmount, gameObject, DamageType.Spike);
        bool contactAccepted = DamageContactHandler != null ? DamageContactHandler(damageable, damageInfo) : damageable.TakeDamage(damageInfo);
        if (!contactAccepted)
            return;

        damagedTargets.Add(damageable);
        StopFromCollision();
    }

    public void PrepareRetract()
    {
        isRetracting = true;
        SetDamageActive(false);
        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
            moveCoroutine = null;
        }
    }

    public void ApplyRetractProgress(float retractProgress)
    {
        retractProgress = Mathf.Clamp01(retractProgress);
        float wavePathProgress = 1f - retractProgress;
        if (isBlocked && wavePathProgress > blockedPathProgress)
        {
            rb.MovePosition(blockedAt);
            return;
        }
        rb.MovePosition(Vector2.Lerp(originPos, targetPos, wavePathProgress));
    }

    private void SetDamageActive(bool active)
    {
        spikeCollider.enabled = active;
        isAttacking = active;
    }

    private void StopFromCollision()
    {
        StopInPlace(true);
        CollisionStopped?.Invoke(this, blockedPathProgress);
    }

    public void StopInPlace(bool playImpactEffect)
    {
        isWarning = false;
        warningBlend = 0f;
        if (warningCoroutine != null)
        {
            StopCoroutine(warningCoroutine);
            warningCoroutine = null;
        }
        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
            moveCoroutine = null;
        }
        SetDamageActive(false);
        isBlocked = true;
        blockedAt = rb.position;
        blockedPathProgress = Mathf.Clamp01(Vector2.Distance(originPos, blockedAt) / Vector2.Distance(originPos, targetPos));
        if (playImpactEffect)
            PlayImpactEffect();
    }

    private void PlayImpactEffect()
    {
        impactCount++;
        impactEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        impactEffect.Play();
        AudioManager.Instance?.PlayTrapSfx(impactSfx, 0.15f);

        CameraShake cameraShake = CameraShake.FindInScene(gameObject.scene);
        cameraShake?.AddShake();
    }

    private IEnumerator WarningFlash(float warningDuration, float warningFlashDuration)
    {
        float timer = 0f;
        while (timer < warningDuration)
        {
            timer += Time.deltaTime;

            float t = Mathf.PingPong(timer / (warningFlashDuration / 2f), 1f);
            warningBlend = t;

            Color lerped = Color.Lerp(originalColor, warningFlashColor, t);
            lerped.a = originalColor.a;

            spriteRenderer.color = lerped;

            yield return null;
        }
        spriteRenderer.color = originalColor;
        warningBlend = 0f;
        warningCoroutine = null;
    }

    private IEnumerator Move(Vector2 origin, Vector2 target, float duration, bool playImpactEffect)
    {
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.fixedDeltaTime;
            float t = Mathf.Clamp01(timer / duration);

            rb.MovePosition(Vector2.Lerp(origin, target, t));

            yield return FixedUpdateWait;
        }
        rb.MovePosition(target);
        if (playImpactEffect)
            PlayImpactEffect();
        moveCoroutine = null;
        SetDamageActive(false);
    }

    public void SetPlaybackControlled(bool controlled)
    {
        ForceIdle();
        rb.interpolation = controlled ? RigidbodyInterpolation2D.None : originalInterpolation;
    }

    public void ApplyPlayback(Vector2 position, bool solid, float flashBlend, bool playImpact)
    {
        // The picture and its colliders use the same position, with no second smoothing pass.
        rb.position = position;
        transform.position = new Vector3(position.x, position.y, transform.position.z);
        SetDamageActive(false);
        solidTipCollider.enabled = solid;
        Color color = Color.Lerp(originalColor, warningFlashColor, flashBlend);
        color.a = originalColor.a;
        spriteRenderer.color = color;
        if (playImpact)
            PlayImpactEffect();
    }

    public bool TryApplyReportedDamage(IDamageable damageable, Vector2 contactPosition)
    {
        // A delayed report must never restart a retraction or resurrect an idle spike.
        if (!isCycleActive || isRetracting || isBlocked || damageable == null || damagedTargets.Contains(damageable))
            return false;
        if (!damageable.TakeDamage(new DamageInfo(damageAmount, gameObject, DamageType.Spike)))
            return false;

        damagedTargets.Add(damageable);
        rb.position = contactPosition;
        rb.MovePosition(contactPosition);
        transform.position = new Vector3(contactPosition.x, contactPosition.y, transform.position.z);
        StopFromCollision();
        return true;
    }

}
