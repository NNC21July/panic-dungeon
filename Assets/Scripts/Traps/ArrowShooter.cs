using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class ArrowShooter : MonoBehaviour
{
    [SerializeField] private Arrow arrowPrefab;
    [SerializeField] private PoisonArrow poisonArrowPrefab;
    [SerializeField, Range(0f, 1f)] private float poisonArrowChance = 0.2f;
    [SerializeField] private Color warningFlashColor;
    [SerializeField] private SpriteRenderer warningLine;
    [SerializeField, Range(0f, 1f)] private float warningLineAlpha = 0.75f;
    [SerializeField] private AudioClip fireSfx;
    [SerializeField] private Sprite chevronSprite;
    [SerializeField, Min(0.01f)] private float chevronSpacing = 1f, chevronSpeed = 4f;
    [SerializeField] private Color chevronColor = Color.white;
    private SpriteRenderer spriteRenderer;
    private Color originalColor, warningLineOriginalColor;
    private Vector2 fireDirection;
    private bool isActive = false;
    private Coroutine warningCoroutine, activationCoroutine;
    private float warningDuration, warningFlashDuration;
    private readonly List<SpriteRenderer> chevrons = new();
    private float localWarningLineLength, chevronStartX, chevronOffset, actualChevronSpacing;
    private int chevronCount;
    public bool IsActive => isActive;

    private void Awake()
    {
        SerializedFieldValidator.Validate(this);

        spriteRenderer = GetComponent<SpriteRenderer>();
        originalColor = spriteRenderer.color;
        warningLineOriginalColor = warningLine.color;
        warningLine.enabled = false;
    }

    public void Configure(Vector2 direction, float worldWarningLineLength, float shooterHalfWidth)
    {
        if (direction == Vector2.zero)
            throw new ArgumentException("Arrow shooter direction cannot be zero");
        fireDirection = direction.normalized;
        float angle = Mathf.Atan2(fireDirection.y, fireDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        float parentScaleX = Mathf.Abs(transform.lossyScale.x),
              originalSpriteWidth = warningLine.sprite.bounds.size.x;
        localWarningLineLength = worldWarningLineLength / parentScaleX;
        Vector3 lineScale = warningLine.transform.localScale;
        lineScale.x = localWarningLineLength / originalSpriteWidth;
        warningLine.transform.localScale = lineScale;

        float localShooterHalfWidth = shooterHalfWidth / parentScaleX;
        Vector3 warningLinePos = warningLine.transform.localPosition;
        warningLinePos.x = localShooterHalfWidth + localWarningLineLength / 2f;
        warningLine.transform.localPosition = warningLinePos;

        chevronStartX = warningLine.transform.localPosition.x - localWarningLineLength / 2f;
        chevronCount = Mathf.Max(1, Mathf.CeilToInt(localWarningLineLength / chevronSpacing));
        actualChevronSpacing = localWarningLineLength / chevronCount;
        RebuildChevrons();
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

        ResetWarningVisuals();
        warningLine.enabled = true;
        SetChevronsVisible(true);
        warningCoroutine = StartCoroutine(WarningFlash());
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

    private void RebuildChevrons()
    {
        foreach (SpriteRenderer chevron in chevrons)
            if (chevron != null)
                Destroy(chevron.gameObject);
        chevrons.Clear();

        for (int i = 0; i < chevronCount; i++)
        {
            GameObject chevronObj = new GameObject("WarningChevron_" + i);
            chevronObj.transform.SetParent(transform, false);

            SpriteRenderer chevronSR = chevronObj.AddComponent<SpriteRenderer>();
            chevronSR.sprite = chevronSprite;
            chevronSR.color = chevronColor;
            chevronSR.sortingOrder = warningLine.sortingOrder + 1;
            chevronSR.enabled = false;

            float warningLineHeight = warningLine.sprite.bounds.size.y * Mathf.Abs(warningLine.transform.localScale.y),
            chevronSpriteHeight = chevronSR.sprite.bounds.size.y,
            chevronScale = warningLineHeight / chevronSpriteHeight;
            chevronObj.transform.localScale = new Vector3(chevronScale, chevronScale, 1f);

            float xPos = chevronStartX + i * actualChevronSpacing;
            chevronObj.transform.localPosition = new Vector3(xPos, warningLine.transform.localPosition.y, 0f);
            chevronObj.transform.localRotation = Quaternion.identity;

            chevrons.Add(chevronSR);
        }
    }

    private void SetChevronsVisible(bool visible)
    {
        foreach (SpriteRenderer chevron in chevrons)
            chevron.enabled = visible;
    }

    private void AnimateChevrons(float dt)
    {
        chevronOffset = Mathf.Repeat(chevronOffset + chevronSpeed * dt, actualChevronSpacing);

        for (int i = 0; i < chevrons.Count; i++)
        {
            SpriteRenderer chevron = chevrons[i];

            Vector3 pos = chevron.transform.localPosition;
            float xPos = chevronStartX + i * actualChevronSpacing + chevronOffset;
            xPos = chevronStartX + Mathf.Repeat(xPos - chevronStartX, localWarningLineLength);
            pos.x = xPos;
            chevron.transform.localPosition = pos;
        }
    }

    private float GetFlashT(float timer)
    {
        return Mathf.PingPong(timer / (warningFlashDuration / 2f), 1f);
    }

    private Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }

    private void ApplyShooterFlash(float flashT)
    {
        Color lerped = Color.Lerp(originalColor, warningFlashColor, flashT);
        lerped.a = originalColor.a;
        spriteRenderer.color = lerped;
    }

    private void ApplyWarningLineFlash(float flashT)
    {
        Color color = Color.Lerp(warningLineOriginalColor, warningFlashColor, flashT);
        color.a = warningLineAlpha * flashT;
        warningLine.color = color;
    }

    private void ApplyChevronFlash(float flashT)
    {
        Color color = WithAlpha(chevronColor, warningLineAlpha * flashT);
        foreach (SpriteRenderer chevron in chevrons)
            chevron.color = color;
    }

    private void ResetWarningVisuals()
    {
        warningLine.color = WithAlpha(warningLineOriginalColor, 0f);
        foreach (SpriteRenderer chevron in chevrons)
            chevron.color = WithAlpha(chevronColor, 0f);

        chevronOffset = 0f;
    }

    private void StopWarningVisuals()
    {
        spriteRenderer.color = originalColor;
        warningLine.color = warningLineOriginalColor;
        warningLine.enabled = false;
        SetChevronsVisible(false);
        warningCoroutine = null;
    }

    private void OnDisable()
    {
        SetChevronsVisible(false);
    }

    private IEnumerator WarningFlash()
    {
        float timer = 0f;
        while (timer < warningDuration)
        {
            float flashT = GetFlashT(timer);

            ApplyShooterFlash(flashT);
            ApplyWarningLineFlash(flashT);
            ApplyChevronFlash(flashT);
            AnimateChevrons(Time.deltaTime);

            timer += Time.deltaTime;

            yield return null;
        }

        StopWarningVisuals();
    }

    private IEnumerator ActivationCycle()
    {
        Warning();
        yield return new WaitUntil(() => warningCoroutine == null);
        Fire();
        isActive = false;
        activationCoroutine = null;
    }
}
