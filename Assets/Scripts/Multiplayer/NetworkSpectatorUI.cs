using System.Collections.Generic;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// A local view of replicated match/health state. Spectating sends no gameplay commands.
public class NetworkSpectatorUI : MonoBehaviour
{
    private NetworkMatchManager manager;
    private CanvasGroup group;
    private TMP_Text caption;
    private Button previousButton, nextButton;
    private NetworkLocalPlayerCamera localCamera;
    private NetworkObject localPlayer, target;
    private readonly List<NetworkObject> livingPlayers = new();
    private bool spectating;

    public static NetworkSpectatorUI Create(NetworkMatchManager manager, TMP_Text styleSource)
    {
        Canvas canvas = styleSource.GetComponentInParent<Canvas>().rootCanvas;
        RectTransform root = MakeRect("SpectatorUI", canvas.transform, Vector2.zero, Vector2.zero, Vector2.zero);
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = root.offsetMax = Vector2.zero;
        NetworkSpectatorUI ui = root.gameObject.AddComponent<NetworkSpectatorUI>();
        ui.manager = manager;
        ui.group = root.gameObject.AddComponent<CanvasGroup>();
        ui.SetVisible(false);
        ui.Build(styleSource.font);
        manager.Runner.AddVisibilityNodes(root.gameObject);
        return ui;
    }

    private void Build(TMP_FontAsset font)
    {
        // Below Leave Game so both remain usable on the upper-left side.
        RectTransform heading = MakeRect("SpectatingLabel", transform, new Vector2(460f, 72f),
            new Vector2(14f, -70f), new Vector2(0f, 1f));
        Image background = heading.gameObject.AddComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 0.5f);
        background.raycastTarget = false;
        caption = MakeText(heading, font, "", 22f);
        caption.alignment = TextAlignmentOptions.MidlineLeft;
        caption.enableAutoSizing = true;
        caption.fontSizeMin = 14f;
        caption.fontSizeMax = 22f;
        previousButton = MakeButton("PreviousPlayer", font, "<", new Vector2(0f, 0.5f), new Vector2(16f, 0f), -1);
        nextButton = MakeButton("NextPlayer", font, ">", new Vector2(1f, 0.5f), new Vector2(-16f, 0f), 1);
    }

    private void Update()
    {
        if (manager == null || manager.Object == null || !manager.Object.IsValid || !manager.Runner.IsRunning)
        {
            SetVisible(false);
            return;
        }
        NetworkRunner runner = manager.Runner;
        if (!runner.TryGetPlayerObject(runner.LocalPlayer, out NetworkObject player) || player == null || !player.IsValid)
        {
            StopSpectating(false);
            return;
        }
        if (localPlayer != player)
        {
            localPlayer = player;
            localCamera = player.GetComponent<NetworkLocalPlayerCamera>();
            target = null;
        }
        NetworkHealth health = player.GetComponent<NetworkHealth>();
        if (health == null || !health.IsDead || manager.State != NetworkMatchState.Playing)
        {
            // Keep the last view behind results; a new match's local camera handles respawning.
            StopSpectating(manager.State != NetworkMatchState.MatchOver);
            return;
        }
        spectating = true;
        RefreshTargets();
        if (target == null || !livingPlayers.Contains(target))
            SelectTarget(livingPlayers.Count > 0 ? livingPlayers[0] : null);
        UpdateCaption();
        bool multipleTargets = livingPlayers.Count > 1;
        previousButton.gameObject.SetActive(multipleTargets);
        nextButton.gameObject.SetActive(multipleTargets);
        SetVisible(runner.GetVisible());
    }

    private void RefreshTargets()
    {
        livingPlayers.Clear();
        foreach (PlayerRef player in manager.Runner.ActivePlayers)
        {
            if (player == manager.Runner.LocalPlayer ||
                !manager.Runner.TryGetPlayerObject(player, out NetworkObject obj) || obj == null || !obj.IsValid)
                continue;
            NetworkHealth health = obj.GetComponent<NetworkHealth>();
            if (health != null && !health.IsDead)
                livingPlayers.Add(obj);
        }
        livingPlayers.Sort((a, b) => a.InputAuthority.AsIndex.CompareTo(b.InputAuthority.AsIndex));
    }

    private void CycleTarget(int direction)
    {
        if (!spectating || manager == null || manager.Object == null || !manager.Object.IsValid ||
            manager.State != NetworkMatchState.Playing || !manager.Runner.GetVisible())
            return;
        // Refresh at click time as a target may have died since the last frame.
        RefreshTargets();
        if (livingPlayers.Count == 0)
        {
            SelectTarget(null);
            return;
        }
        int index = livingPlayers.IndexOf(target);
        index = index < 0 ? (direction > 0 ? 0 : livingPlayers.Count - 1)
            : (index + direction + livingPlayers.Count) % livingPlayers.Count;
        SelectTarget(livingPlayers[index]);
        UpdateCaption();
    }

    private void SelectTarget(NetworkObject player)
    {
        target = player;
        if (localCamera != null)
            localCamera.FollowTarget(target != null ? target.transform : null);
    }

    private void UpdateCaption()
    {
        string remaining = $"{manager.LivingPlayerCount}/{manager.StartingPlayerCount} remaining";
        if (target == null)
        {
            caption.text = $"Spectating: waiting for results...\n{remaining}";
            return;
        }
        NetworkPlayerIdentity identity = target.GetComponent<NetworkPlayerIdentity>();
        string name = identity != null ? identity.DisplayName : $"Player {target.InputAuthority.AsIndex + 1}";
        caption.text = $"Spectating: {name}\n{remaining}";
    }

    private void StopSpectating(bool restoreCamera)
    {
        if (spectating && restoreCamera && localCamera != null && localPlayer != null && localPlayer.IsValid)
            localCamera.FollowTarget(localPlayer.transform);
        spectating = false;
        target = null;
        livingPlayers.Clear();
        SetVisible(false);
    }

    private void OnDestroy()
    {
        StopSpectating(true);
    }

    private void SetVisible(bool visible)
    {
        group.alpha = visible ? 1f : 0f;
        group.interactable = visible;
        group.blocksRaycasts = visible;
    }

    private Button MakeButton(string name, TMP_FontAsset font, string symbol, Vector2 anchor, Vector2 position, int direction)
    {
        RectTransform rect = MakeRect(name, transform, new Vector2(56f, 72f), position, anchor);
        Image background = rect.gameObject.AddComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 0.5f);
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = background;
        button.onClick.AddListener(() => CycleTarget(direction));
        MakeText(rect, font, symbol, 36f);
        return button;
    }

    private static RectTransform MakeRect(string name, Transform parent, Vector2 size, Vector2 position, Vector2 anchor)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.layer = parent.gameObject.layer;
        RectTransform rect = (RectTransform)obj.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        return rect;
    }

    private static TMP_Text MakeText(RectTransform parent, TMP_FontAsset font, string value, float size)
    {
        RectTransform rect = MakeRect("Text", parent, Vector2.zero, Vector2.zero, Vector2.one * 0.5f);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(10f, 4f);
        rect.offsetMax = new Vector2(-10f, -4f);
        TMP_Text text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.font = font;
        text.text = value;
        text.fontSize = size;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        text.richText = false;
        text.raycastTarget = false;
        return text;
    }
}
