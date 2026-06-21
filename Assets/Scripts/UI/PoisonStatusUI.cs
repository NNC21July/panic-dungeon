using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class PoisonStatusUI : MonoBehaviour
{
    [SerializeField] private StatusEffectController statusEffectController;
    [SerializeField] private TMP_Text poisonStatusTxt;
    [SerializeField, Min(0.01f)] private float flashColorDuration;
    [SerializeField] private Color flashColor;
    private Coroutine flashColorCoroutine;
    private Color originalColor;

    private void Awake()
    {
        SerializedFieldValidator.Validate(this);
        poisonStatusTxt.gameObject.SetActive(false);
        originalColor = poisonStatusTxt.color;
    }

    private void OnEnable()
    {
        statusEffectController.EffectStarted += ShowUI;
        statusEffectController.EffectEnded += HideUI;

        if (statusEffectController.IsEffectActive(typeof(PoisonEffect)))
            ShowUI(typeof(PoisonEffect));
        else
            HideUI(typeof(PoisonEffect));
    }

    private void OnDisable()
    {
        HideUI(typeof(PoisonEffect));
        statusEffectController.EffectStarted -= ShowUI;
        statusEffectController.EffectEnded -= HideUI;
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