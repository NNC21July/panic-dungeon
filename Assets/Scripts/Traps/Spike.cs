using System.Collections;
using UnityEngine;

public class Spike : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    [SerializeField] private float warningDuration = 2f, warningFlashSpeed = 0.25f;
    [SerializeField] private Color warningFlashColor;
    private Color originalColor;
    private Coroutine warningCoroutine;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalColor = spriteRenderer.color;
    }

    public void Warning()
    {
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

            float halfFlashDuration = warningFlashSpeed / 2f;

            float t = Mathf.PingPong(timer / halfFlashDuration, 1f);

            Color lerped = Color.Lerp(originalColor, warningFlashColor, t);
            lerped.a = originalColor.a;

            spriteRenderer.color = lerped;

            yield return null;
        }
        spriteRenderer.color = originalColor;
        warningCoroutine = null;
    }
}
