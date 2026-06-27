using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class ArrowShooter : MonoBehaviour
{
    [SerializeField] private Arrow arrowPrefab;
    [SerializeField] private PoisonArrow poisonArrowPrefab;
    [SerializeField, Range(0f, 1f)] private float poisonArrowChance = 0.2f;
    [SerializeField] private Color warningFlashColor;
    [SerializeField] private SpriteRenderer warningLine;
    [SerializeField, Range(0f, 1f)] private float warningLineColor;
    [SerializeField] private AudioClip fireSfx;
    private SpriteRenderer spriteRenderer;
    private Color originalColor, warningLineOriginalColor;
    private Vector2 fireDirection;
    private bool isActive = false;
    private Coroutine warningCoroutine, activationCoroutine;
    private float warningDuration, warningFlashDuration;
    public bool IsActive => isActive;

    private void Awake()
    {
        SerializedFieldValidator.Validate(this);

        spriteRenderer = GetComponent<SpriteRenderer>();
        originalColor = spriteRenderer.color;
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

    public bool Activate(float newWarningDuration, float newWarningFlashDuration)
    {
        if (fireDirection == Vector2.zero)
            throw new ArgumentException(nameof(fireDirection));

        if (isActive)
            return false;

        warningDuration = Mathf.Max(0.01f, newWarningDuration);
        warningFlashDuration = Mathf.Max(0.01f, newWarningFlashDuration);

        isActive = true;
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
        Arrow arrowInstance = Instantiate(SelectArrow(), transform.position, Quaternion.identity);

        arrowInstance.Initialize(fireDirection, gameObject);

        AudioManager.Instance?.PlayTrapSfx(fireSfx, 0.15f);
    }

    private Arrow SelectArrow()
    {
        if (UnityEngine.Random.value < poisonArrowChance)
            return poisonArrowPrefab;
        return arrowPrefab;
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
        warningLine.enabled = false;
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