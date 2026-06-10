using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(PolygonCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class Spike : MonoBehaviour
{
    private static readonly WaitForFixedUpdate FixedUpdateWait = new WaitForFixedUpdate();
    private SpriteRenderer spriteRenderer;
    [SerializeField, Min(0.01f)] private float warningDuration = 2f, warningFlashDuration = 0.25f, moveDuration = 5f;
    [SerializeField, Min(0f)] private float damageAmount = 25f, retractDelay = 2f;
    [SerializeField] private Color warningFlashColor;
    private Color originalColor;
    private Coroutine warningCoroutine, moveCoroutine, activationCoroutine;
    private bool isAttacking = false, isConfigured = false;
    private PolygonCollider2D spikeCollider;
    private Rigidbody2D rb;
    [SerializeField] private Vector2 originPos, targetPos;
    private HashSet<EntityId> damagedTargetIDs;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        spikeCollider = GetComponent<PolygonCollider2D>();
        rb = GetComponent<Rigidbody2D>();
        damagedTargetIDs = new HashSet<EntityId>();
        originalColor = spriteRenderer.color;
        SetDamageActive(false);
    }

    public void ConfigurePos(Vector2 origin, Vector2 target)
    {
        originPos = origin;
        targetPos = target;
        isConfigured = true;
    }

    public void Activate()
    {
        if (!isConfigured)
            throw new InvalidOperationException("Spike must be configured before activation");

        if (activationCoroutine != null)
            StopCoroutine(activationCoroutine);
        Idle();
        activationCoroutine = StartCoroutine(ActivationCycle());
    }

    private void Idle()
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
        damagedTargetIDs.Clear();
    }

    private void Warning()
    {
        SetDamageActive(false);

        if (warningCoroutine != null)
            StopCoroutine(warningCoroutine);
        warningCoroutine = StartCoroutine(WarningFlash());
    }

    IEnumerator WarningFlash()
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

    private void Attack()
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
        if (!isAttacking)
            return;

        IDamageable damageable = other.GetComponentInParent<IDamageable>();
        if (damageable == null)
            return;

        EntityId otherID = other.GetComponentInParent<EntityId>();
        if (otherID == null)
            return;
        if (damagedTargetIDs.Contains(otherID))
            return;


        damageable.TakeDamage(new DamageInfo(damageAmount, gameObject, DamageType.Spike));
        damagedTargetIDs.Add(otherID);
    }

    private void Retract()
    {
        SetDamageActive(false);

        if (moveCoroutine != null)
            StopCoroutine(moveCoroutine);
        moveCoroutine = StartCoroutine(Move(targetPos, originPos, moveDuration));
    }

    private void SetDamageActive(bool active)
    {
        spikeCollider.enabled = active;
        isAttacking = active;
    }

    IEnumerator Move(Vector2 origin, Vector2 target, float duration)
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
    }

    IEnumerator ActivationCycle()
    {
        Idle();
        Warning();
        yield return new WaitUntil(() => warningCoroutine == null);
        Attack();
        yield return new WaitUntil(() => moveCoroutine == null);
        SetDamageActive(false);
        yield return new WaitForSeconds(retractDelay);
        Retract();
        yield return new WaitUntil(() => moveCoroutine == null);
        Idle();
        activationCoroutine = null;
    }
}
