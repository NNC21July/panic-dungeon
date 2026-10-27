using System.Collections.Generic;
using DG.Tweening;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Presentation only: the server owns the results, each client draws the same table.
public class NetworkResultsUI : MonoBehaviour
{
    private NetworkMatchManager manager;
    private CanvasGroup group;
    private RectTransform panel, progressFill;
    private TMP_Text heading, subtitle, note, transitionText;
    private Button restart;
    private readonly List<MatchPlayerResult> sortedResults = new();
    private readonly List<TMP_Text[]> rows = new();
    private readonly List<TMP_Text> awards = new();
    private readonly List<CanvasGroup> awardGroups = new();
    private readonly List<Sequence> scoreTweens = new();
    private ResultsPresentationSettings presentation;
    private readonly List<Image> medals = new();
    private readonly List<ResultsUsernameShine> usernameShines = new();
    private readonly List<Sequence> podiumTweens = new();
    private readonly List<float> podiumStarts = new();
    private readonly List<int> podiumRows = new();
    private int presentedRound = -1;
    private bool presentedFinal, wasVisible;
    private MatchPlayerResult[] previewResults;
    private float previewStarted;
    private bool IsPreview => previewResults != null;
    private float PresentationElapsed => IsPreview ? Time.unscaledTime - previewStarted :
        Mathf.Max(0f, manager.ResultsDisplayDuration - manager.ResultsSecondsRemaining);

    public static NetworkResultsUI CreatePreview(TMP_Text styleSource, MatchPlayerResult[] results)
    {
        NetworkResultsUI ui = Create(null, styleSource);
        ui.previewResults = results;
        ui.restart.onClick.AddListener(ui.ReplayPreview);
        ui.restart.GetComponentInChildren<TMP_Text>().text = "REPLAY ANIMATION";
        ui.ReplayPreview();
        return ui;
    }

    private void ReplayPreview()
    {
        previewStarted = Time.unscaledTime;
        presentedFinal = true;
        presentedRound = 3;
        foreach (Image medal in medals)
            medal.rectTransform.sizeDelta = presentation.medalSize;
        PresentResults(true);
    }

    public static NetworkResultsUI Create(NetworkMatchManager manager, TMP_Text styleSource)
    {
        Canvas canvas = styleSource.GetComponentInParent<Canvas>().rootCanvas;
        RectTransform root = Rect("MatchResults", canvas.transform);
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.sizeDelta = Vector2.zero;
        NetworkResultsUI ui = root.gameObject.AddComponent<NetworkResultsUI>();
        ui.manager = manager;
        ui.presentation = Resources.Load<ResultsPresentationSettings>("ResultsPresentationSettings");
        if (ui.presentation == null)
            ui.presentation = ScriptableObject.CreateInstance<ResultsPresentationSettings>();
        ui.group = root.gameObject.AddComponent<CanvasGroup>();
        ui.Show(false);
        ui.Build(styleSource.font);
        if (manager != null)
            manager.Runner.AddVisibilityNodes(root.gameObject);
        return ui;
    }

    private void Build(TMP_FontAsset font)
    {
        panel = Rect("ResultsTable", transform);
        panel.sizeDelta = new Vector2(1100f, 690f);
        panel.gameObject.AddComponent<Image>().color = new Color(0.035f, 0.045f, 0.07f, 0.96f);
        heading = Text("Heading", panel, font, "MATCH RESULTS", 30f);
        heading.rectTransform.anchoredPosition = new Vector2(0f, 295f);
        heading.rectTransform.sizeDelta = new Vector2(1044f, 46f);
        heading.alignment = TextAlignmentOptions.Center;
        subtitle = Text("Subtitle", panel, font, "", 22f);
        subtitle.rectTransform.anchoredPosition = new Vector2(0f, 250f);
        subtitle.rectTransform.sizeDelta = new Vector2(1044f, 38f);
        subtitle.alignment = TextAlignmentOptions.Center;
        TMP_Text[] header = MakeRow(panel, font, 200f);
        string[] titles = { "RANK", "USERNAME", "DAMAGE", "KILLS", "DURATION", "POINTS" };
        for (int i = 0; i < titles.Length; i++)
        {
            header[i].text = titles[i];
            header[i].fontSize = 17f;
            header[i].color = new Color(0.7f, 0.75f, 0.85f);
        }
        for (int i = 0; i < 10; i++)
        {
            TMP_Text[] row = MakeRow(panel, font, 155f - i * 40f);
            rows.Add(row);
            RectTransform medalRect = Rect("Medal", row[0].transform);
            medalRect.sizeDelta = presentation.medalSize;
            Image medal = medalRect.gameObject.AddComponent<Image>();
            medal.preserveAspect = true;
            medal.raycastTarget = false;
            medal.gameObject.SetActive(false);
            medals.Add(medal);
            ResultsUsernameShine shine = row[1].gameObject.AddComponent<ResultsUsernameShine>();
            shine.enabled = false;
            usernameShines.Add(shine);
            TMP_Text award = Text("PointsAward", row[5].transform, font, "", 19f);
            award.rectTransform.anchorMin = award.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            award.rectTransform.pivot = new Vector2(1f, 0.5f);
            award.rectTransform.sizeDelta = new Vector2(60f, 38f);
            award.alignment = TextAlignmentOptions.MidlineRight;
            award.color = new Color(0.55f, 1f, 0.72f);
            awards.Add(award);
            CanvasGroup awardGroup = award.gameObject.AddComponent<CanvasGroup>();
            awardGroup.alpha = 0f;
            awardGroups.Add(awardGroup);
        }
        note = Text("ScoringNote", panel, font, "", 16f);
        note.rectTransform.sizeDelta = new Vector2(1044f, 28f);
        note.rectTransform.anchoredPosition = new Vector2(0f, -250f);
        note.alignment = TextAlignmentOptions.Center;
        transitionText = Text("NextRound", panel, font, "", 20f);
        transitionText.rectTransform.sizeDelta = new Vector2(1044f, 30f);
        transitionText.rectTransform.anchoredPosition = new Vector2(0f, -287f);
        transitionText.alignment = TextAlignmentOptions.Center;
        RectTransform track = Rect("TransitionProgress", panel);
        track.sizeDelta = new Vector2(1100f, 8f);
        track.anchoredPosition = new Vector2(0f, -365f);
        track.gameObject.AddComponent<Image>().color = new Color(0.12f, 0.15f, 0.2f);
        progressFill = Rect("Fill", track);
        progressFill.anchorMin = Vector2.zero;
        progressFill.anchorMax = Vector2.one;
        progressFill.offsetMin = progressFill.offsetMax = Vector2.zero;
        progressFill.gameObject.AddComponent<Image>().color = new Color(0.35f, 0.7f, 1f);
        RectTransform button = Rect("RetryTransition", panel);
        button.sizeDelta = new Vector2(220f, 30f);
        button.anchoredPosition = new Vector2(0f, -322f);
        Image background = button.gameObject.AddComponent<Image>();
        background.color = new Color(0.18f, 0.32f, 0.52f);
        restart = button.gameObject.AddComponent<Button>();
        restart.targetGraphic = background;
        if (manager != null)
            restart.onClick.AddListener(manager.RestartMatch);
        TMP_Text label = Text("Label", button, font, "RETRY", 18f);
        label.rectTransform.sizeDelta = button.sizeDelta;
        label.alignment = TextAlignmentOptions.Center;
        restart.gameObject.SetActive(false);
    }

    private void Update()
    {
        bool visible = IsPreview || (manager != null && manager.Object != null && manager.Object.IsValid &&
            manager.State == NetworkMatchState.MatchOver && manager.Runner.GetVisible());
        Show(visible);
        if (!visible)
        {
            if (wasVisible)
                FinishAnimations();
            wasVisible = false;
            return;
        }
        wasVisible = true;
        // Fit small windows without squeezing away the username column.
        Rect bounds = ((RectTransform)transform).rect;
        float scale = Mathf.Min(1f, Mathf.Min((bounds.width - 24f) / 1100f, (bounds.height - 24f) / 770f));
        panel.localScale = Vector3.one * Mathf.Max(0.1f, scale);
        if (IsPreview)
        {
            UpdatePodium();
            progressFill.anchorMax = new Vector2(Mathf.Clamp01(PresentationElapsed / 12f), 1f);
            transitionText.text = "PREVIEW — edit ResultsPresentationSettings, then replay";
            restart.gameObject.SetActive(true);
            restart.interactable = true;
            return;
        }
        if (presentedRound != manager.CurrentRound || presentedFinal != manager.SeriesComplete)
        {
            bool animate = presentedRound != manager.CurrentRound;
            presentedRound = manager.CurrentRound;
            presentedFinal = manager.SeriesComplete;
            PresentResults(animate);
        }
        float remaining = manager.ResultsSecondsRemaining;
        UpdatePodium();
        progressFill.anchorMax = new Vector2(manager.ResultsProgress, 1f);
        if (manager.TransitionFailed)
            transitionText.text = "Scene transition failed. The room leader can retry.";
        else if (manager.Restarting)
            transitionText.text = manager.SeriesComplete ? "Returning to lobby..." : "Loading next round...";
        else
            transitionText.text = manager.SeriesComplete
                ? $"Returning to lobby in {Mathf.CeilToInt(remaining)}s"
                : $"Round {manager.CurrentRound + 1} / {manager.RoundLimit} begins in {Mathf.CeilToInt(remaining)}s";
        restart.gameObject.SetActive(manager.TransitionFailed && manager.IsLocalLeader);
        restart.interactable = !manager.Restarting;
    }

    private void PresentResults(bool animate)
    {
        FinishAnimations();
        ResetPodium();
        sortedResults.Clear();
        if (IsPreview)
            sortedResults.AddRange(previewResults);
        else
            for (int i = 0; i < manager.ResultCount; i++)
                sortedResults.Add(manager.Results[i]);
        sortedResults.Sort((a, b) =>
        {
            int comparison = presentedFinal
                ? MatchPointRules.CompareSeries(a.TotalPoints, a.TotalDuration, b.TotalPoints, b.TotalDuration)
                : (bool)a.Participated != (bool)b.Participated ? (a.Participated ? -1 : 1) : a.Placement.CompareTo(b.Placement);
            return comparison != 0 ? comparison : a.Player.AsIndex.CompareTo(b.Player.AsIndex);
        });
        heading.text = presentedFinal ? "FINAL MATCH RESULTS" : $"ROUND {manager.CurrentRound} / {manager.RoundLimit} RESULTS";
        subtitle.text = IsPreview ? "Preview" : manager.Winner == PlayerRef.None ? "Round ended in a draw" : $"Round winner: {manager.WinnerName}";
        if (presentedFinal)
        {
            bool tied = sortedResults.Count > 1 && MatchPointRules.CompareSeries(sortedResults[0].TotalPoints,
                sortedResults[0].TotalDuration, sortedResults[1].TotalPoints, sortedResults[1].TotalDuration) == 0;
            subtitle.text = sortedResults.Count == 0 ? "No results" : tied ? "Joint winners — tied on points and survival" : $"Winner: {sortedResults[0].Username}";
        }
        note.text = presentedFinal
            ? "Series totals • Ranked by points, then total survival • Player damage and kills only"
            : "Round stats • Placement awards points • POINTS shows your cumulative total";
        int rank = 1;
        for (int i = 0; i < rows.Count; i++)
        {
            rows[i][0].transform.parent.gameObject.SetActive(i < sortedResults.Count);
            if (i >= sortedResults.Count)
                continue;
            MatchPlayerResult result = sortedResults[i];
            if (presentedFinal && i > 0 && MatchPointRules.CompareSeries(result.TotalPoints, result.TotalDuration,
                sortedResults[i - 1].TotalPoints, sortedResults[i - 1].TotalDuration) != 0)
                rank = i + 1;
            rows[i][0].text = presentedFinal ? rank.ToString() : result.Participated ? result.Placement.ToString() : "—";
            rows[i][1].text = result.Username.ToString();
            rows[i][2].text = (presentedFinal ? result.TotalDamage : result.Damage).ToString("0.#");
            rows[i][3].text = (presentedFinal ? result.TotalKills : result.Kills).ToString();
            int tenths = Mathf.FloorToInt((presentedFinal ? result.TotalDuration : result.Duration) * 10f);
            rows[i][4].text = $"{tenths / 600:00}:{tenths / 10 % 60:00}.{tenths % 10}";
            rows[i][5].text = result.TotalPoints.ToString();
            if (animate && result.RoundPoints > 0)
                AnimatePoints(i, result);
            if (presentedFinal && (IsPreview || manager.CurrentRound >= manager.RoundLimit) && rank <= 3)
                PreparePodium(i, rank);
        }
    }

    private void AnimatePoints(int index, MatchPlayerResult result)
    {
        float animationDuration = Mathf.Max(0.1f, presentation.pointsDuration);
        float animationDelay = Mathf.Max(0f, presentation.pointsDelay);
        float elapsed = PresentationElapsed;
        if (elapsed >= animationDelay + animationDuration)
            return;
        TMP_Text total = rows[index][5], award = awards[index];
        CanvasGroup alpha = awardGroups[index];
        int displayed = result.TotalPoints - result.RoundPoints;
        total.text = displayed.ToString();
        award.text = $"+{result.RoundPoints}";
        award.rectTransform.anchoredPosition = Vector2.zero;
        award.rectTransform.localScale = Vector3.one * 0.8f;
        alpha.alpha = 0f;
        Sequence sequence = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
        sequence.Insert(0f, alpha.DOFade(1f, animationDuration * (0.15f / 1.4f)));
        sequence.Insert(0f, award.rectTransform.DOScale(presentation.pointsScale, animationDuration).SetEase(Ease.OutQuad));
        sequence.Insert(0f, award.rectTransform.DOAnchorPosY(presentation.pointsRise, animationDuration).SetEase(Ease.OutQuad));
        sequence.Insert(animationDuration * (0.15f / 1.4f), DOTween.To(() => displayed, value =>
        {
            displayed = value;
            total.text = value.ToString();
        }, result.TotalPoints, animationDuration / 1.4f).SetEase(Ease.OutCubic));
        sequence.Insert(animationDuration * (0.6f / 1.4f), alpha.DOFade(0f, animationDuration * (0.8f / 1.4f)));
        sequence.OnComplete(() => total.text = result.TotalPoints.ToString());
        scoreTweens.Add(sequence);
        // A late snapshot/view switch catches up instead of replaying an old award.
        sequence.PrependInterval(animationDelay);
        sequence.Goto(elapsed, true);
    }

    private void FinishAnimations()
    {
        foreach (Sequence sequence in scoreTweens)
            sequence.Kill();
        scoreTweens.Clear();
        for (int i = 0; i < rows.Count; i++)
        {
            if (awardGroups[i] != null)
                awardGroups[i].alpha = 0f;
            if (i < sortedResults.Count && rows[i][5] != null)
                rows[i][5].text = sortedResults[i].TotalPoints.ToString();
        }
    }

    private void OnDisable() => FinishAnimations();
    private void OnDestroy()
    {
        FinishAnimations();
        ResetPodium();
    }

    private void PreparePodium(int index, int rank)
    {
        Image medal = medals[index];
        Sprite sprite = presentation.medals != null && presentation.medals.Length >= rank ? presentation.medals[rank - 1] : null;
        // Keep the numeric rank as a fallback if a medal asset has been removed.
        if (sprite != null)
        {
            medal.sprite = sprite;
            medal.rectTransform.localScale = Vector3.zero;
            medal.gameObject.SetActive(true);
        }
        ResultsUsernameShine shine = usernameShines[index];
        shine.Initialize(presentation, rank);
        shine.enabled = true;
        float blend = 0f;
        Sequence sequence = DOTween.Sequence().SetAutoKill(false).Pause();
        sequence.Insert(0f, medal.rectTransform.DOScale(1f, Mathf.Max(0.1f, presentation.medalDuration)).SetEase(presentation.medalEase));
        sequence.Insert(0f, DOTween.To(() => blend, value => blend = value, 1f,
            Mathf.Max(0.1f, presentation.gradientDuration)).SetEase(Ease.OutSine));
        // The same server-results clock drives all reveals, including late view switches.
        float start = Mathf.Max(0f, presentation.pointsDelay) + Mathf.Max(0.1f, presentation.pointsDuration) +
            Mathf.Max(0f, presentation.medalDelayAfterPoints) + (rank - 1) * Mathf.Max(0f, presentation.rankStagger);
        sequence.OnUpdate(() => shine.SetPresentation(
            Mathf.Max(0f, PresentationElapsed - start), blend));
        podiumTweens.Add(sequence);
        podiumStarts.Add(start);
        podiumRows.Add(index);
    }

    private void UpdatePodium()
    {
        float elapsed = PresentationElapsed;
        for (int i = 0; i < podiumTweens.Count; i++)
        {
            float time = Mathf.Max(0f, elapsed - podiumStarts[i]);
            int row = podiumRows[i];
            if (time > 0f && medals[row].gameObject.activeSelf && medals[row].sprite != null)
                rows[row][0].text = "";
            podiumTweens[i].Goto(time, false);
            // Keep sweeping after the one-shot reveal sequence has finished.
            if (time >= podiumTweens[i].Duration())
                usernameShines[podiumRows[i]].SetPresentation(time, 1f);
        }
    }

    private void ResetPodium()
    {
        foreach (Sequence sequence in podiumTweens)
            sequence.Kill();
        podiumTweens.Clear();
        podiumStarts.Clear();
        podiumRows.Clear();
        for (int i = 0; i < medals.Count; i++)
        {
            if (medals[i] != null)
                medals[i].gameObject.SetActive(false);
            if (usernameShines[i] != null)
            {
                usernameShines[i].SetPresentation(0f, 0f);
                usernameShines[i].enabled = false;
            }
        }
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
        row.sizeDelta = new Vector2(1044f, 38f);
        row.anchoredPosition = new Vector2(0f, y);
        HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 16f;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = layout.childForceExpandHeight = false;
        float[] widths = { 60f, 0f, 100f, 62f, 116f, 150f };
        TMP_Text[] cells = new TMP_Text[6];
        for (int i = 0; i < cells.Length; i++)
        {
            cells[i] = Text("Cell", row, font, "", 22f);
            cells[i].alignment = i == 0 ? TextAlignmentOptions.Center :
                i == 1 ? TextAlignmentOptions.MidlineLeft : TextAlignmentOptions.MidlineRight;
            cells[i].overflowMode = TextOverflowModes.Ellipsis;
            cells[i].textWrappingMode = TextWrappingModes.NoWrap;
            if (i == 5)
                cells[i].margin = new Vector4(0f, 0f, 64f, 0f);
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
