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
    private Vector2 originPos, targetPos, blockedAt = new Vector2(0, 0);
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private Coroutine warningCoroutine, moveCoroutine, retractCoroutine;
    private bool isAttacking = false, isBlocked = false;
    private PolygonCollider2D spikeCollider;
    private Rigidbody2D rb;
    private HashSet<IDamageable> damagedTargets;
    private float blockedPathProgress;
    private const float laneTolerance = 0.01f;
    public float MoveDuration => moveDuration;

    private void Awake()
    {
        SerializedFieldValidator.Validate(this);
        spriteRenderer = GetComponent<SpriteRenderer>();
        spikeCollider = GetComponent<PolygonCollider2D>();
        rb = GetComponent<Rigidbody2D>();
        damagedTargets = new HashSet<IDamageable>();
        originalColor = spriteRenderer.color;
        SetDamageActive(false);
    }

    public void ConfigurePos(Vector2 origin, Vector2 target)
    {
        originPos = origin;
        targetPos = target;
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
        if (retractCoroutine != null)
        {
            StopCoroutine(retractCoroutine);
            retractCoroutine = null;
        }
        SetDamageActive(false);
        rb.MovePosition(originPos);
        spriteRenderer.color = originalColor;
        damagedTargets.Clear();
        isBlocked = false;
        blockedPathProgress = 0f;
        blockedAt = originPos;
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
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isAttacking || isBlocked)
            return;

        if (other.gameObject.layer == GameLayers.Obstacle) // stops when hitting an obstacle
        {
            if (Mathf.Abs(other.transform.position.x - originPos.x) > laneTolerance) // check if obstacle is same lane with spike
                return;
            StopInPlace(true);
            return;
        }

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

    public void BeginRetractWithWave()
    {
        SetDamageActive(false);

        if (retractCoroutine != null)
            StopCoroutine(retractCoroutine);
        retractCoroutine = StartCoroutine(RetractWithWave());
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
        if (retractCoroutine != null)
        {
            StopCoroutine(retractCoroutine);
            retractCoroutine = null;
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
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySfx(impactSfx, 0.15f);
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

    private IEnumerator RetractWithWave()
    {
        if (isBlocked)
        {
            float waitTime = moveDuration * (1f - blockedPathProgress),
                retractTime = moveDuration - waitTime;

            yield return new WaitForSeconds(waitTime);

            if (moveCoroutine != null)
                StopCoroutine(moveCoroutine);
            moveCoroutine = StartCoroutine(Move(blockedAt, originPos, retractTime, false));
        }
        else
        {
            if (moveCoroutine != null)
                StopCoroutine(moveCoroutine);
            moveCoroutine = StartCoroutine(Move(targetPos, originPos, moveDuration, false));
        }
        yield return new WaitUntil(() => moveCoroutine == null);
        retractCoroutine = null;
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
