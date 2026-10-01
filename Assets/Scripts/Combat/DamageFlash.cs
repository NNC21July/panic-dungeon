using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class DamageFlash : MonoBehaviour
{
    [SerializeField] private Color flashColor;
    [SerializeField] private float flashDuration;
    private Health health;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private Coroutine flashCoroutine;
    private NetworkHealth networkHealth;

    private void Awake()
    {
        networkHealth = GetComponent<NetworkHealth>();
        if (networkHealth == null)
            health = GetComponent<Health>();
        if (networkHealth == null && health == null)
        {
            Debug.LogError("DamageFlash requires either Health or NetworkHealth.", this);
            enabled = false;
            return;
        }
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalColor = spriteRenderer.color;
    }

    private void OnEnable()
    {
        if (networkHealth != null)
            networkHealth.OnDamaged += HandleNetworkDamaged;
        else if (health != null)
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
        if (networkHealth != null)
            networkHealth.OnDamaged -= HandleNetworkDamaged;
        else if (health != null)
            health.OnDamaged -= HandleDamaged;
    }

    private void HandleNetworkDamaged()
    {
        if (networkHealth.SuppressPresentedFlash)
            return;
        PlayFlash();
    }

    private void HandleDamaged(DamageInfo _)
    {
        PlayFlash();
    }

    public void PlayFlash()
    {
        if (!isActiveAndEnabled)
            return;
        if (flashCoroutine != null)
            StopCoroutine(flashCoroutine);
        flashCoroutine = StartCoroutine(Flash());
    }

    private IEnumerator Flash()
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
