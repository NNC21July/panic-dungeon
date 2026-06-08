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

    void Awake()
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
        health.OnDamaged -= HandleDamaged;
    }

    private void HandleDamaged(DamageInfo damageInfo)
    {
        StopAllCoroutines();
        StartCoroutine(Flash());
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
    }
}
