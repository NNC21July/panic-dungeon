using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class ArrowShooter : MonoBehaviour, ITrap
{
    [SerializeField] private Arrow arrowPrefab;
    [SerializeField, Min(0.01f)] private float warningDuration = 0.6f, warningFlashDuration = 0.5f;
    [SerializeField] private Color warningFlashColor;
    [SerializeField] private SpriteRenderer warningLine;
    [SerializeField, Range(0f, 1f)] private float warningLineColor;
    private SpriteRenderer spriteRenderer;
    private Color originalColor, warningLineOriginalColor;
    private Vector2 fireDirection;
    private bool isActive = false;
    public bool IsActive => isActive;
    private Coroutine warningCoroutine, activationCoroutine;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalColor = spriteRenderer.color;

        if (warningLine == null)
            throw new ArgumentNullException("Warning line must be assigned in inspector");

        warningLineOriginalColor = warningLine.color;
        warningLine.enabled = false;
    }

    public void Configure(Vector2 direction)
    {
        if (direction == Vector2.zero)
            throw new ArgumentException("Arrow shooter direction cannot be zero");
        fireDirection = direction.normalized;
        float angle = Mathf.Atan2(fireDirection.y, fireDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    public bool Activate(float newWarningDuration)
    {
        if (arrowPrefab == null)
            throw new ArgumentNullException("Arrow prefab must be assigned in the inspector");

        if (fireDirection == Vector2.zero)
            throw new ArgumentException("Arrow shooter direction cannot be zero");

        if (isActive)
            return false;

        warningDuration = Mathf.Max(0.01f, newWarningDuration);

        isActive = true;
        if (activationCoroutine != null)
            StopCoroutine(activationCoroutine);
        activationCoroutine = StartCoroutine(ActivationCycle());
        return true;
    }

    private void Warning()
    {
        if (warningCoroutine != null)
            StopCoroutine(warningCoroutine);
        warningCoroutine = StartCoroutine(WarningFlash());
        warningLine.enabled = true;
    }

    private void Fire()
    {
        Arrow arrowInstance = Instantiate(arrowPrefab, transform.position, Quaternion.identity);

        arrowInstance.Initialize(fireDirection);
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

            Color warningFlashColorTrans = warningFlashColor;
            warningFlashColorTrans.a = warningLineColor;
            Color warningLineLerped = Color.Lerp(warningLineOriginalColor, warningFlashColorTrans, t);
            warningLine.color = warningLineLerped;

            yield return null;
        }
        spriteRenderer.color = originalColor;
        warningLine.color = warningLineOriginalColor;
        warningCoroutine = null;
    }

    private IEnumerator ActivationCycle()
    {
        Warning();
        yield return new WaitUntil(() => warningCoroutine == null);
        warningLine.enabled = false;
        spriteRenderer.color = originalColor;
        Fire();
        isActive = false;
        activationCoroutine = null;
    }
}