using UnityEngine;
using UnityEngine.UI;

public static class CornerBorderUI
{
    public static void Create(RectTransform parent, Vector2 size, float length, float thickness)
    {
        length = Mathf.Min(length, Mathf.Min(size.x, size.y) / 2f);
        for (int x = -1; x <= 1; x += 2)
            for (int y = -1; y <= 1; y += 2)
            {
                Vector2 anchor = new Vector2((x + 1) / 2f, (y + 1) / 2f);
                MakeLine(parent, anchor, new Vector2(-x * length / 2f, -y * thickness / 2f),
                    new Vector2(length, thickness));
                MakeLine(parent, anchor, new Vector2(-x * thickness / 2f, -y * length / 2f),
                    new Vector2(thickness, length));
            }
    }

    private static void MakeLine(RectTransform parent, Vector2 anchor, Vector2 position, Vector2 size)
    {
        GameObject obj = new GameObject("Corner", typeof(RectTransform));
        obj.layer = parent.gameObject.layer;
        RectTransform rect = (RectTransform)obj.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        Image image = obj.AddComponent<Image>();
        image.color = Color.white;
        image.raycastTarget = false;
    }
}
