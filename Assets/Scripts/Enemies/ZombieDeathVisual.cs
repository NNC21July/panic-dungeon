using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class ZombieDeathVisual : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private DamageFlash damageFlash;
    private Color livingColor;
    private bool deathShown;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        damageFlash = GetComponent<DamageFlash>();
        livingColor = spriteRenderer.color;
    }

    public void ResetVisual()
    {
        deathShown = false;
        spriteRenderer.color = livingColor;
        if (damageFlash != null)
            damageFlash.enabled = true;
    }

    public void ShowDeath(float brightness, float opacity)
    {
        if (!deathShown)
        {
            deathShown = true;
            if (damageFlash != null)
                damageFlash.enabled = false;
        }
        brightness = Mathf.Clamp01(brightness);
        spriteRenderer.color = new Color(livingColor.r * brightness, livingColor.g * brightness,
            livingColor.b * brightness, livingColor.a * Mathf.Clamp01(opacity));
    }
}
