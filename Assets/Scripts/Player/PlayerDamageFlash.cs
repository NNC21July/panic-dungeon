using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Health))]
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerDamageFlash : MonoBehaviour
{
    private Health health;
    private SpriteRenderer spriteRenderer;
    [SerializeField] private Color flashColor;
    [SerializeField] private float flashDuration;
    private Color originalColor;
    private Coroutine flashCoroutine;

    private void Awake()
    {
        health = GetComponent<Health>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalColor = spriteRenderer.color;
    }

    private void OnEnable()
    {
        health.OnDamaged += HandleDamaged;
    }

    private void OnDisable()
    {
        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
            flashCoroutine = null;
        }
        spriteRenderer.color = originalColor;
        health.OnDamaged -= HandleDamaged;
    }

    private void HandleDamaged(DamageInfo damageInfo)
    {
        if (flashCoroutine != null)
            StopCoroutine(flashCoroutine);
        flashCoroutine = StartCoroutine(Flash());
    }

    IEnumerator Flash()
    {
        float flashTimer = flashDuration;
        while (flashTimer > 0f)
        {
            flashTimer -= Time.deltaTime;
            Color lerped = Color.Lerp(flashColor, originalColor, 1 - flashTimer / flashDuration);
            lerped.a = originalColor.a;

            spriteRenderer.color = lerped;

            yield return null;
        }
        spriteRenderer.color = originalColor;
        flashCoroutine = null;
    }
}
