using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;

public class NetworkPerformanceUI : MonoBehaviour
{
    private NetworkRunner runner;
    private TMP_Text label;
    private float elapsed;
    private int frames;

    public static NetworkPerformanceUI Create(NetworkRunner runner, TMP_Text styleSource)
    {
        Canvas canvas = styleSource.GetComponentInParent<Canvas>().rootCanvas;
        GameObject obj = new GameObject("PerformanceLabel", typeof(RectTransform));
        obj.layer = canvas.gameObject.layer;
        obj.transform.SetParent(canvas.transform, false);
        RectTransform rect = (RectTransform)obj.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 0f);
        rect.anchoredPosition = new Vector2(-12f, 10f);
        rect.sizeDelta = new Vector2(260f, 28f);
        TextMeshProUGUI text = obj.AddComponent<TextMeshProUGUI>();
        text.font = styleSource.font;
        text.fontSize = 18f;
        text.color = new Color(1f, 1f, 1f, 0.75f);
        text.alignment = TextAlignmentOptions.BottomRight;
        text.raycastTarget = false;
        text.text = "FPS: --   Ping: --";
        NetworkPerformanceUI ui = obj.AddComponent<NetworkPerformanceUI>();
        ui.runner = runner;
        ui.label = text;
        runner.AddVisibilityNodes(obj);
        return ui;
    }

    private void Update()
    {
        frames++;
        elapsed += Time.unscaledDeltaTime;
        if (elapsed < 0.5f)
            return;
        int fps = Mathf.RoundToInt(frames / elapsed);
        string ping = "--";
        if (runner != null && runner.IsRunning && runner.IsConnectedToServer && runner.LocalPlayer != PlayerRef.None)
        {
            double milliseconds = runner.GetPlayerRtt(runner.LocalPlayer) * 1000d;
            if (!double.IsNaN(milliseconds) && !double.IsInfinity(milliseconds) && milliseconds >= 0d)
                ping = $"{milliseconds:F0} ms";
        }
        label.text = $"FPS: {fps}   Ping: {ping}";
        frames = 0;
        elapsed = 0f;
    }
}
