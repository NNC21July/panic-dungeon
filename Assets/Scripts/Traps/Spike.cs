using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(PolygonCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class Spike : MonoBehaviour, ITrap
{
    private static readonly WaitForFixedUpdate FixedUpdateWait = new WaitForFixedUpdate();
    [SerializeField, Min(0.01f)] private float warningDuration = 2f, warningFlashDuration = 0.25f, moveDuration = 5f;
    [SerializeField, Min(0f)] private float damageAmount = 25f, retractDelay = 2f;
    [SerializeField] private Color warningFlashColor;
    private Vector2 originPos, targetPos, blockedAt = new Vector2(0, 0);
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private Coroutine warningCoroutine, moveCoroutine, retractCoroutine;
    private bool isAttacking = false, isConfigured = false, isBlocked = false;
    private PolygonCollider2D spikeCollider;
    private Rigidbody2D rb;
    private HashSet<IDamageable> damagedTargets;
    private float blockedPathProgress;
    private const float laneTolerance = 0.01f;
    public bool IsActive => warningCoroutine != null || moveCoroutine != null || retractCoroutine != null;
    public float MoveDuration => moveDuration;
    public float RetractDelay => retractDelay;

    private void Awake()
    {
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
        isConfigured = true;
    }

    public bool Activate(float newWarningDuration)
    {
        if (!isConfigured)
            throw new InvalidOperationException("Spike must be configured before activation");

        if (IsActive)
            return false;

        BeginWarning(newWarningDuration);
        return true;
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

    public void BeginWarning(float newWarningDuration)
    {
        warningDuration = Mathf.Max(0.01f, newWarningDuration);
        SetDamageActive(false);

        if (warningCoroutine != null)
            StopCoroutine(warningCoroutine);
        warningCoroutine = StartCoroutine(WarningFlash());
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
        moveCoroutine = StartCoroutine(Move(originPos, targetPos, moveDuration));
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isAttacking || isBlocked)
            return;

        if (other.gameObject.layer == LayerMask.NameToLayer("Obstacle")) // stops when hitting an obstacle
        {
            if (Mathf.Abs(other.transform.position.x - originPos.x) > laneTolerance) // check if obstacle is same lane with spike
                return;
            if (moveCoroutine != null)
            {
                StopCoroutine(moveCoroutine);
                moveCoroutine = null;
            }
            SetDamageActive(false);
            isBlocked = true;
            blockedAt = rb.position;
            blockedPathProgress = Mathf.Clamp01(Vector2.Distance(originPos, blockedAt) / Vector2.Distance(originPos, targetPos));
            return;
        }

        IDamageable damageable = other.GetComponentInParent<IDamageable>();
        if (damageable == null)
            return;

        if (damagedTargets.Contains(damageable))
            return;

        damageable.TakeDamage(new DamageInfo(damageAmount, gameObject, DamageType.Spike));
        damagedTargets.Add(damageable);
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

    public void StopInPlace()
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
        spriteRenderer.color = originalColor;
    }

    private IEnumerator WarningFlash()
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
            moveCoroutine = StartCoroutine(Move(blockedAt, originPos, retractTime));
        }
        else
        {
            if (moveCoroutine != null)
                StopCoroutine(moveCoroutine);
            moveCoroutine = StartCoroutine(Move(targetPos, originPos, moveDuration));
        }
        yield return new WaitUntil(() => moveCoroutine == null);
        retractCoroutine = null;
    }


    private IEnumerator Move(Vector2 origin, Vector2 target, float duration)
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
        moveCoroutine = null;
        SetDamageActive(false);
    }

}
