using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spike : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    [SerializeField] private float warningDuration = 2f, warningFlashDuration = 0.25f, damage = 25f, moveDuration = 5f, retractDelay = 2f;
    [SerializeField] private Color warningFlashColor;
    private Color originalColor;
    private Coroutine warningCoroutine, moveCoroutine, activationCoroutine;
    private bool isAttacking = false;
    private PolygonCollider2D spikeCollider;
    private Rigidbody2D rb;
    public Vector2 originPos, targetPos;
    private HashSet<EntityId> damagedTargetIDs;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        spikeCollider = GetComponent<PolygonCollider2D>();
        rb = GetComponent<Rigidbody2D>();
        damagedTargetIDs = new HashSet<EntityId>();
    }

    void Start()
    {
        originalColor = spriteRenderer.color;
        spikeCollider.enabled = false;
        isAttacking = false;
    }

    public void Activate()
    {
        if (activationCoroutine != null)
            StopCoroutine(activationCoroutine);
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
        spikeCollider.enabled = false;
        isAttacking = false;
        rb.MovePosition(originPos);
        spriteRenderer.color = originalColor;
        damagedTargetIDs.Clear();
    }

    private void Warning()
    {
        spikeCollider.enabled = false;
        isAttacking = false;

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
        spikeCollider.enabled = true;
        isAttacking = true;

        if (moveCoroutine != null)
            StopCoroutine(moveCoroutine);
        moveCoroutine = StartCoroutine(Move(originPos, targetPos, moveDuration));
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isAttacking)
            return;

        if (damagedTargetIDs.Contains(other.GetEntityId()))
            return;

        IDamageable damageable = other.GetComponent<IDamageable>();
        if (damageable == null)
            return;

        damageable.TakeDamage(new DamageInfo(damage, gameObject, DamageType.Spike));
        damagedTargetIDs.Add(other.GetEntityId());
    }

    private void Retract()
    {
        spikeCollider.enabled = false;
        isAttacking = false;

        if (moveCoroutine != null)
            StopCoroutine(moveCoroutine);
        moveCoroutine = StartCoroutine(Move(targetPos, originPos, moveDuration));
    }

    IEnumerator Move(Vector2 origin, Vector2 target, float duration)
    {
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.fixedDeltaTime;
            float t = Mathf.Clamp01(timer / duration);

            rb.MovePosition(Vector2.Lerp(origin, target, t));

            yield return new WaitForFixedUpdate();
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
        yield return new WaitForSeconds(retractDelay);
        Retract();
        yield return new WaitUntil(() => moveCoroutine == null);
        Idle();
        activationCoroutine = null;
    }
}
