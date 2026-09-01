using System;
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
    private NetworkHealth networkHealth;

    private void Awake()
    {
        if (vignetteImage == null)
            throw new InvalidOperationException("DamageVignetteUI requires a vignette Image.");
        SetAlpha(0f);
    }

    private void OnEnable()
    {
        Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
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

    private void PlayVignette()
    {
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        SetAlpha(maxAlpha);
        fadeCoroutine = StartCoroutine(FadeOut());
    }

    public void Bind(NetworkHealth target)
    {
        if (target == null)
            throw new ArgumentNullException(nameof(target));

        Unsubscribe();
        networkHealth = target;
        if (isActiveAndEnabled)
            Subscribe();
    }

    private void Subscribe()
    {
        if (networkHealth != null)
            networkHealth.OnDamaged += HandleNetworkDamaged;
        else if (playerHealth != null)
            playerHealth.OnDamaged += HandleDamaged;
    }

    private void Unsubscribe()
    {
        if (networkHealth != null)
            networkHealth.OnDamaged -= HandleNetworkDamaged;
        else if (playerHealth != null)
            playerHealth.OnDamaged -= HandleDamaged;
    }

    private void HandleNetworkDamaged()
    {
        PlayVignette();
    }

    private void HandleDamaged(DamageInfo _)
    {
        PlayVignette();
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