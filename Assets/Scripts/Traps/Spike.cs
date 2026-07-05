using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(PolygonCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class Spike : MonoBehaviour
{
    private static readonly WaitForFixedUpdate FixedUpdateWait = new WaitForFixedUpdate();
    [SerializeField, Min(0.01f)] private float moveDuration = 5f;
    [SerializeField, Min(0f)] private float damageAmount = 25f;
    [SerializeField] private Color warningFlashColor;
    [SerializeField] private ParticleSystem impactEffect;
    [SerializeField] private AudioClip impactSfx;
    [SerializeField] private Collider2D solidTipCollider;
    [SerializeField] private Transform tipPoint;
    [SerializeField] private SpriteRenderer pillarBody;
    private Vector2 originPos, targetPos, blockedAt = new Vector2(0, 0);
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private Coroutine warningCoroutine, moveCoroutine;
    private bool isAttacking = false, isBlocked = false;
    private PolygonCollider2D spikeCollider;
    private Rigidbody2D rb;
    private HashSet<IDamageable> damagedTargets;
    private float blockedPathProgress;
    private const float laneTolerance = 0.01f;
    public float MoveDuration => moveDuration;
    public Vector2 TipPos => tipPoint.position;

    private void Awake()
    {
        SerializedFieldValidator.Validate(this);
        spriteRenderer = GetComponent<SpriteRenderer>();
        spikeCollider = GetComponent<PolygonCollider2D>();
        rb = GetComponent<Rigidbody2D>();
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
        newWarningDuration = Mathf.Max(0.01f, newWarningDuration);
        newWarningFlashDuration = Mathf.Max(0.01f, newWarningFlashDuration);
        SetDamageActive(false);

        if (warningCoroutine != null)
            StopCoroutine(warningCoroutine);
        warningCoroutine = StartCoroutine(WarningFlash(newWarningDuration, newWarningFlashDuration));
    }

    public void BeginAttack()
    {
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
        if (!isAttacking || isBlocked)
            return;

        int otherLayer = other.gameObject.layer;

        if (otherLayer == GameLayers.Obstacle) // stops when hitting an obstacle
        {
            if (Mathf.Abs(other.transform.position.x - originPos.x) > laneTolerance) // check if obstacle is same lane with spike
                return;
            StopInPlace(true);
            return;
        }

        if (otherLayer != GameLayers.Player && otherLayer != GameLayers.Enemy)
            return;

        IDamageable damageable = other.GetComponentInParent<IDamageable>();
        if (damageable == null)
            return;

        if (damagedTargets.Contains(damageable))
            return;

        if (!damageable.TakeDamage(new DamageInfo(damageAmount, gameObject, DamageType.Spike)))
            return;
        damagedTargets.Add(damageable);
        StopInPlace(true);
    }

    public void PrepareRetract()
    {
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

    public void StopInPlace(bool playImpactEffect)
    {
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
        impactEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        impactEffect.Play();
        AudioManager.Instance?.PlayTrapSfx(impactSfx, 0.15f);
        CameraShake.Instance?.AddShake();
    }

    private IEnumerator WarningFlash(float warningDuration, float warningFlashDuration)
    {
        float timer = 0f;
        while (timer < warningDuration)
        {
            timer += Time.deltaTime;

            float t = Mathf.PingPong(timer / (warningFlashDuration / 2f), 1f);

            Color lerped = Color.Lerp(originalColor, warningFlashColor, t);
            lerped.a = originalColor.a;

            spriteRenderer.color = lerped;

            yield return null;
        }
        spriteRenderer.color = originalColor;
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

}
