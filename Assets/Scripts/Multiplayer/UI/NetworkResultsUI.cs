using System.Collections.Generic;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Presentation only: the server owns the results, each client draws the same table.
public class NetworkResultsUI : MonoBehaviour
{
    private NetworkMatchManager manager;
    private CanvasGroup group;
    private RectTransform panel;
    private TMP_Text heading;
    private Button restart;
    private readonly List<MatchPlayerResult> sortedResults = new();
    private readonly List<TMP_Text[]> rows = new();

    public static NetworkResultsUI Create(NetworkMatchManager manager, TMP_Text styleSource)
    {
        Canvas canvas = styleSource.GetComponentInParent<Canvas>().rootCanvas;
        RectTransform root = Rect("MatchResults", canvas.transform);
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.sizeDelta = Vector2.zero;
        NetworkResultsUI ui = root.gameObject.AddComponent<NetworkResultsUI>();
        ui.manager = manager;
        ui.group = root.gameObject.AddComponent<CanvasGroup>();
        ui.Show(false);
        ui.Build(styleSource.font);
        manager.Runner.AddVisibilityNodes(root.gameObject);
        return ui;
    }

    private void Build(TMP_FontAsset font)
    {
        panel = Rect("ResultsTable", transform);
        panel.sizeDelta = new Vector2(900f, 650f);
        panel.gameObject.AddComponent<Image>().color = new Color(0.035f, 0.045f, 0.07f, 0.96f);
        heading = Text("Heading", panel, font, "MATCH RESULTS", 30f);
        heading.rectTransform.anchoredPosition = new Vector2(0f, 275f);
        heading.rectTransform.sizeDelta = new Vector2(850f, 60f);
        heading.alignment = TextAlignmentOptions.Center;
        TMP_Text[] header = MakeRow(panel, font, 215f);
        string[] titles = { "RANK", "USERNAME", "DAMAGE", "KILLS", "DURATION" };
        for (int i = 0; i < titles.Length; i++)
        {
            header[i].text = titles[i];
            header[i].fontSize = 17f;
            header[i].color = new Color(0.7f, 0.75f, 0.85f);
        }
        for (int i = 0; i < 10; i++)
            rows.Add(MakeRow(panel, font, 170f - i * 40f));
        TMP_Text note = Text("ScoringNote", panel, font, "Player damage and kills only • Ranked by survival time", 16f);
        note.rectTransform.sizeDelta = new Vector2(850f, 28f);
        note.rectTransform.anchoredPosition = new Vector2(0f, -240f);
        note.alignment = TextAlignmentOptions.Center;
        RectTransform button = Rect("Restart", panel);
        button.sizeDelta = new Vector2(220f, 42f);
        button.anchoredPosition = new Vector2(0f, -285f);
        Image background = button.gameObject.AddComponent<Image>();
        background.color = new Color(0.18f, 0.32f, 0.52f);
        restart = button.gameObject.AddComponent<Button>();
        restart.targetGraphic = background;
        restart.onClick.AddListener(manager.RestartMatch);
        TMP_Text label = Text("Label", button, font, "RESTART", 20f);
        label.rectTransform.sizeDelta = button.sizeDelta;
        label.alignment = TextAlignmentOptions.Center;
    }

    private void Update()
    {
        bool visible = manager != null && manager.Object != null && manager.Object.IsValid &&
            manager.State == NetworkMatchState.MatchOver && manager.Runner.GetVisible();
        Show(visible);
        if (!visible)
            return;
        // Fit small windows without squeezing away the username column.
        Rect bounds = ((RectTransform)transform).rect;
        float scale = Mathf.Min(1f, Mathf.Min((bounds.width - 24f) / 900f, (bounds.height - 24f) / 650f));
        panel.localScale = Vector3.one * Mathf.Max(0.1f, scale);
        heading.text = manager.Winner == PlayerRef.None ? "MATCH RESULTS — DRAW" : $"Winner: {manager.WinnerName}";
        sortedResults.Clear();
        for (int i = 0; i < manager.ResultCount; i++)
            sortedResults.Add(manager.Results[i]);
        sortedResults.Sort((a, b) =>
        {
            int duration = b.Duration.CompareTo(a.Duration);
            return duration != 0 ? duration : a.Player.AsIndex.CompareTo(b.Player.AsIndex);
        });
        int rank = 1;
        for (int i = 0; i < rows.Count; i++)
        {
            rows[i][0].transform.parent.gameObject.SetActive(i < sortedResults.Count);
            if (i >= sortedResults.Count)
                continue;
            MatchPlayerResult result = sortedResults[i];
            // Equal survival times share a rank; join ID keeps their display order stable.
            if (i > 0 && result.Duration != sortedResults[i - 1].Duration)
                rank = i + 1;
            rows[i][0].text = rank.ToString();
            rows[i][1].text = result.Username.ToString();
            rows[i][2].text = result.Damage.ToString("0.#");
            rows[i][3].text = result.Kills.ToString();
            int tenths = Mathf.FloorToInt(result.Duration * 10f);
            rows[i][4].text = $"{tenths / 600:00}:{tenths / 10 % 60:00}.{tenths % 10}";
        }
        restart.gameObject.SetActive(manager.IsLocalLeader);
        restart.interactable = !manager.Restarting;
    }

    private void Show(bool visible)
    {
        group.alpha = visible ? 1f : 0f;
        group.interactable = visible;
        group.blocksRaycasts = visible;
    }

    private static TMP_Text[] MakeRow(Transform parent, TMP_FontAsset font, float y)
    {
        RectTransform row = Rect("Row", parent);
        row.sizeDelta = new Vector2(844f, 38f);
        row.anchoredPosition = new Vector2(0f, y);
        HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 16f;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = layout.childForceExpandHeight = false;
        float[] widths = { 60f, 0f, 112f, 70f, 118f };
        TMP_Text[] cells = new TMP_Text[5];
        for (int i = 0; i < cells.Length; i++)
        {
            cells[i] = Text("Cell", row, font, "", 22f);
            cells[i].alignment = i < 2 ? TextAlignmentOptions.MidlineLeft : TextAlignmentOptions.MidlineRight;
            cells[i].overflowMode = TextOverflowModes.Ellipsis;
            cells[i].textWrappingMode = TextWrappingModes.NoWrap;
            LayoutElement sizing = cells[i].gameObject.AddComponent<LayoutElement>();
            sizing.minWidth = sizing.preferredWidth = widths[i];
            sizing.flexibleWidth = i == 1 ? 1f : 0f;
            sizing.preferredHeight = 38f;
        }
        return cells;
    }

    private static RectTransform Rect(string name, Transform parent)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.layer = parent.gameObject.layer;
        RectTransform rect = (RectTransform)obj.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * 0.5f;
        return rect;
    }

    private static TMP_Text Text(string name, Transform parent, TMP_FontAsset font, string value, float size)
    {
        TMP_Text text = Rect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
        text.font = font;
        text.text = value;
        text.fontSize = size;
        text.color = Color.white;
        text.richText = false;
        text.raycastTarget = false;
        return text;
    }
}
