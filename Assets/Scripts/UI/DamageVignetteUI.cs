using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class DamageVignetteUI : MonoBehaviour
{
    [SerializeField] private Health playerHealth;
    [SerializeField] private Image vignetteImage;
    [SerializeField, Min(0.01f)] private float fadeDuration = 0.35f;
    [SerializeField, Range(0f, 1f)] private float maxAlpha = 0.45f;
    private Coroutine fadeCoroutine;

    private void Awake()
    {
        SerializedFieldValidator.Validate(this);
        SetAlpha(0f);
    }

    private void OnEnable()
    {
        playerHealth.OnDamaged += HandleDamaged;
    }

    private void OnDisable()
    {
        playerHealth.OnDamaged -= HandleDamaged;
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }
        SetAlpha(0f);
    }

    private void SetAlpha(float alpha)
    {
        Color temp = vignetteImage.color;
        temp.a = alpha;
        vignetteImage.color = temp;
    }

    private void HandleDamaged(DamageInfo damageInfo)
    {
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        SetAlpha(maxAlpha);
        fadeCoroutine = StartCoroutine(FadeOut());
    }

    private IEnumerator FadeOut()
    {
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(timer / fadeDuration);
            SetAlpha(Mathf.Lerp(maxAlpha, 0f, t));

            yield return null;
        }
        SetAlpha(0f);
        fadeCoroutine = null;
    }
}