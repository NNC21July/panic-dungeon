using System;
using System.Collections;
using TMPro;
using NaughtyAttributes;
using UnityEngine;

public class PoisonStatusUI : MonoBehaviour
{
    [Tooltip("Single-player reference. Multiplayer assigns the viewed player at runtime; leave empty there.")]
    [SerializeField] private StatusEffectController statusEffectController;
    [SerializeField, Required] private TMP_Text poisonStatusTxt;
    [SerializeField, Min(0.01f)] private float flashColorDuration;
    [SerializeField] private Color flashColor;
    private Coroutine flashColorCoroutine;
    private Color originalColor;
    private NetworkHealth networkHealth;

    private void Awake()
    {
        if (poisonStatusTxt == null)
            throw new InvalidOperationException("PoisonStatusUI requires poisonStatusTxt.");
        poisonStatusTxt.gameObject.SetActive(false);
        originalColor = poisonStatusTxt.color;
    }

    private void OnEnable()
    {
        Subscribe();
        RefreshStatus();
    }

    private void OnDisable()
    {
        HideUI(typeof(PoisonEffect));
        Unsubscribe();
    }

    public void Bind(NetworkHealth target)
    {
        Unsubscribe();
        networkHealth = target;
        statusEffectController = null;
        if (isActiveAndEnabled)
        {
            Subscribe();
            RefreshStatus();
        }
    }

    private void Subscribe()
    {
        if (networkHealth != null)
            networkHealth.OnPoisonChanged += RefreshStatus;
        else if (statusEffectController != null)
        {
            statusEffectController.EffectStarted += ShowUI;
            statusEffectController.EffectEnded += HideUI;
        }
    }

    private void Unsubscribe()
    {
        if (networkHealth != null)
            networkHealth.OnPoisonChanged -= RefreshStatus;
        else if (statusEffectController != null)
        {
            statusEffectController.EffectStarted -= ShowUI;
            statusEffectController.EffectEnded -= HideUI;
        }
    }

    private void RefreshStatus()
    {
        bool active = networkHealth != null
            ? networkHealth.Object != null && networkHealth.Object.IsValid && networkHealth.IsPoisoned
            : statusEffectController != null && statusEffectController.IsEffectActive(typeof(PoisonEffect));
        if (active)
            ShowUI(typeof(PoisonEffect));
        else
            HideUI(typeof(PoisonEffect));
    }

    private void ShowUI(Type effectType)
    {
        if (effectType != typeof(PoisonEffect))
            return;

        poisonStatusTxt.gameObject.SetActive(true);
        if (flashColorCoroutine != null)
            StopCoroutine(flashColorCoroutine);
        flashColorCoroutine = StartCoroutine(FlashColor());
    }

    private void HideUI(Type effectType)
    {
        if (effectType != typeof(PoisonEffect))
            return;

        poisonStatusTxt.gameObject.SetActive(false);
        if (flashColorCoroutine != null)
        {
            StopCoroutine(flashColorCoroutine);
            flashColorCoroutine = null;
        }
        poisonStatusTxt.color = originalColor;
    }

    private IEnumerator FlashColor()
    {
        while (true)
        {
            float timer = 0f;
            while (timer < flashColorDuration)
            {
                timer += Time.deltaTime;

                float t = Mathf.PingPong(timer / (flashColorDuration / 2f), 1f);

                Color lerped = Color.Lerp(originalColor, flashColor, t);
                lerped.a = originalColor.a;

                poisonStatusTxt.color = lerped;

                yield return null;
            }
            poisonStatusTxt.color = originalColor;
        }
    }
}
