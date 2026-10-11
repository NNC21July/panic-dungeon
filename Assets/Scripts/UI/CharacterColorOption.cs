using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CharacterColorOption : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private RectTransform circle, shimmer;
    private SelectionCircleGraphic circleGraphic;
    private CanvasGroup corners;
    private TMP_Text label;
    private Button button;
    private Color baseColor;
    private bool hovered, selected, available;
    private float shineTime = 1f;

    public void Configure(RectTransform circle, SelectionCircleGraphic graphic, RectTransform shimmer,
        CanvasGroup corners, TMP_Text label, Button button, Color color)
    {
        this.circle = circle;
        circleGraphic = graphic;
        this.shimmer = shimmer;
        this.corners = corners;
        this.label = label;
        this.button = button;
        baseColor = color;
    }

    public void SetState(bool mine, bool taken, bool ready, bool canChoose)
    {
        if (mine && !selected)
            shineTime = 0f;
        selected = mine;
        available = canChoose && !taken;
        button.interactable = available;
        label.text = taken ? "PICKED" : mine ? (ready ? "READY" : "SELECTED") : "";
        label.color = taken ? new Color(0.7f, 0.72f, 0.78f) : Color.white;
    }

    private void Update()
    {
        float blend = 1f - Mathf.Exp(-15f * Time.unscaledDeltaTime);
        float scale = hovered && available ? 1.12f : selected ? 1.04f : 1f;
        circle.localScale = Vector3.Lerp(circle.localScale, Vector3.one * scale, blend);
        Color targetColor = baseColor;
        if (!available && !selected)
            targetColor = Color.Lerp(baseColor, new Color(0.16f, 0.17f, 0.21f), 0.7f);
        circleGraphic.color = Color.Lerp(circleGraphic.color, targetColor, blend);
        corners.alpha = Mathf.Lerp(corners.alpha, selected ? 1f : 0f, blend);
        shineTime += Time.unscaledDeltaTime / 0.55f;
        bool shining = selected && shineTime < 1f;
        shimmer.gameObject.SetActive(shining);
        if (shining)
        {
            float t = Mathf.SmoothStep(0f, 1f, shineTime);
            shimmer.anchoredPosition = new Vector2(Mathf.Lerp(-95f, 95f, t), 0f);
        }
    }

    public void OnPointerEnter(PointerEventData eventData) => hovered = true;
    public void OnPointerExit(PointerEventData eventData) => hovered = false;
    private void OnDisable() => hovered = false;
}
