using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class HazardWarningUI : MonoBehaviour
{
    [SerializeField] private Image spikeWarningIcon;
    [SerializeField, Min(0.01f)] private float flashDuration = 0.4f;
    private Coroutine spikeFlashCoroutine;

    private void Awake()
    {
        SerializedFieldValidator.Validate(this);

        HideAllWarnings();
    }

    public void ShowSpikeWarning(bool isTopSpike)
    {
        RectTransform rectTransform = spikeWarningIcon.rectTransform;

        if (isTopSpike)
        {
            rectTransform.anchorMin = new Vector2(0.5f, 1f);
            rectTransform.anchorMax = new Vector2(0.5f, 1f);
            rectTransform.anchoredPosition = new Vector2(0f, -200f);
        }
        else
        {
            rectTransform.anchorMin = new Vector2(0.5f, 0f);
            rectTransform.anchorMax = new Vector2(0.5f, 0f);
            rectTransform.anchoredPosition = new Vector2(0f, 200f);
        }

        if (spikeFlashCoroutine != null)
            StopCoroutine(spikeFlashCoroutine);
        spikeFlashCoroutine = StartCoroutine(FlashSpikeWarning());
    }

    public void HideAllWarnings()
    {
        HideSpikeWarning();
    }

    public void HideSpikeWarning()
    {
        if (spikeFlashCoroutine != null)
        {
            StopCoroutine(spikeFlashCoroutine);
            spikeFlashCoroutine = null;
        }
        SetSpikeWarningAlpha(0f);
    }

    private void SetSpikeWarningAlpha(float alpha)
    {
        Color color = spikeWarningIcon.color;
        color.a = alpha;
        spikeWarningIcon.color = color;
    }

    private IEnumerator FlashSpikeWarning()
    {
        float timer = 0f;
        while (true)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.PingPong(timer / (flashDuration / 2f), 1f);
            SetSpikeWarningAlpha(alpha);

            yield return null;
        }
    }
}