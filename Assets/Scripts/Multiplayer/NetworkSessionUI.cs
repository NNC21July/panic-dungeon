using System.Collections.Generic;
using System.Text;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Local UI only. Requests still pass through server-side lobby/match validation.
public class NetworkSessionUI : MonoBehaviour
{
    private NetworkRunner runner;
    private NetworkLobbyManager lobby;
    private CanvasGroup group;
    private RectTransform content;
    private GameObject confirmation;
    private TMP_Text roster, room, status, prompt;
    private Button startButton;
    private bool previousInput;
    private readonly List<PlayerRef> players = new();
    private readonly StringBuilder rows = new();

    public static NetworkSessionUI Create(NetworkRunner runner, TMP_Text styleSource, NetworkLobbyManager lobby = null)
    {
        Canvas parent = styleSource.GetComponentInParent<Canvas>().rootCanvas;
        RectTransform root = MakeRect("SessionUI", parent.transform, Vector2.zero, Vector2.zero);
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = root.offsetMax = Vector2.zero;
        // Above character selection and match results, but scoped to this runner's scene.
        Canvas canvas = root.gameObject.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = parent.sortingOrder + 100;
        root.gameObject.AddComponent<GraphicRaycaster>();
        NetworkSessionUI ui = root.gameObject.AddComponent<NetworkSessionUI>();
        ui.runner = runner;
        ui.lobby = lobby;
        ui.group = root.gameObject.AddComponent<CanvasGroup>();
        ui.Build(styleSource.font);
        runner.AddVisibilityNodes(root.gameObject);
        return ui;
    }

    private void Build(TMP_FontAsset font)
    {
        if (lobby != null)
        {
            MakeBackdrop(transform, new Color(0.015f, 0.02f, 0.035f, 1f));
            content = MakeRect("LobbyContent", transform, new Vector2(740f, 650f), Vector2.zero);
            MakeText(content, font, "MULTIPLAYER LOBBY", 34f, new Vector2(700f, 50f), new Vector2(0f, 280f));
            room = MakeText(content, font, "", 22f, new Vector2(700f, 40f), new Vector2(0f, 230f));
            roster = MakeText(content, font, "", 23f, new Vector2(620f, 350f), new Vector2(0f, 20f));
            roster.alignment = TextAlignmentOptions.TopLeft;
            roster.lineSpacing = 8f;
            roster.enableAutoSizing = true;
            roster.fontSizeMin = 16f;
            roster.fontSizeMax = 23f;
            status = MakeText(content, font, "", 18f, new Vector2(680f, 65f), new Vector2(0f, -185f));
            startButton = MakeButton(content, font, "START GAME", new Vector2(-150f, -265f), () => lobby.RequestStartGame());
            MakeButton(content, font, "LEAVE ROOM", new Vector2(150f, -265f), ShowConfirmation);
        }
        else
        {
            Button leave = MakeButton(transform, font, "LEAVE GAME", Vector2.zero, ShowConfirmation);
            RectTransform rect = (RectTransform)leave.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(14f, -14f);
            rect.sizeDelta = new Vector2(180f, 44f);
        }
        confirmation = MakeBackdrop(transform, new Color(0f, 0f, 0f, 0.9f)).gameObject;
        RectTransform dialog = MakeRect("LeaveConfirmation", confirmation.transform, new Vector2(680f, 300f), Vector2.zero);
        prompt = MakeText(dialog, font, "", 24f, new Vector2(640f, 160f), new Vector2(0f, 55f));
        MakeButton(dialog, font, "CANCEL", new Vector2(-150f, -90f), HideConfirmation);
        MakeButton(dialog, font, "LEAVE", new Vector2(150f, -90f), () => NetworkSessionBootstrap.Instance?.LeaveSession());
        confirmation.SetActive(false);
    }

    private void Update()
    {
        bool visible = runner != null && runner.IsRunning && !runner.IsServer && runner.GetVisible();
        bool returning = NetworkSessionBootstrap.Instance != null && NetworkSessionBootstrap.Instance.IsReturning;
        group.alpha = visible ? 1f : 0f;
        group.interactable = visible && !returning;
        group.blocksRaycasts = visible;
        if (!visible)
        {
            if (confirmation.activeSelf)
                HideConfirmation();
            return;
        }
        Rect bounds = ((RectTransform)transform).rect;
        float scale = Mathf.Min(1f, bounds.width / 780f, bounds.height / 700f);
        if (content != null)
            content.localScale = Vector3.one * scale;
        confirmation.transform.GetChild(0).localScale = Vector3.one * scale;
        if (returning && confirmation.activeSelf)
            prompt.text = "Leaving session...";
        if (lobby == null || lobby.Object == null || !lobby.Object.IsValid)
            return;
        players.Clear();
        foreach (PlayerRef player in runner.ActivePlayers)
            if (player != PlayerRef.None)
                players.Add(player);
        players.Sort((a, b) => a.AsIndex.CompareTo(b.AsIndex));
        rows.Clear();
        foreach (PlayerRef player in players)
        {
            rows.Append("Player ").Append(player.AsIndex + 1);
            if (player == runner.LocalPlayer)
                rows.Append("  -  You");
            if (player == lobby.Leader)
                rows.Append("  -  Leader");
            if (!runner.TryGetPlayerObject(player, out NetworkObject obj) || obj == null || !obj.IsValid)
                rows.Append("  (loading)");
            rows.AppendLine();
        }
        roster.text = rows.ToString();
        room.text = $"Room: {runner.SessionInfo.Name}    |    {players.Count}/10 players";
        startButton.gameObject.SetActive(lobby.IsLocalLeader);
        startButton.interactable = lobby.CanRequestStart && !confirmation.activeSelf;
        status.text = lobby.StartingGame ? "Loading game..." :
            lobby.StartError != LobbyStartError.None ? "Game could not load. Check the scene configuration and try again." :
            players.Count < lobby.MinimumPlayers ? $"Waiting for at least {lobby.MinimumPlayers} players..." :
            lobby.ReadyPlayers < lobby.ConnectedPlayers ? $"Waiting for players to load ({lobby.ReadyPlayers}/{lobby.ConnectedPlayers})..." :
            lobby.IsLocalLeader ? "Start when everyone has joined. Color selection comes next." : "Waiting for the lobby leader to start.";
    }

    private void ShowConfirmation()
    {
        previousInput = runner.ProvideInput;
        runner.ProvideInput = false;
        runner.GetComponent<NetworkInputProvider>()?.ClearMenuInput();
        bool creator = NetworkSessionBootstrap.Instance != null && NetworkSessionBootstrap.Instance.HasLocalServer;
        prompt.text = creator
            ? "Close this room?\nThis computer runs the server. Leaving ends the session for everyone."
            : "Leave this room and return to Create / Join?";
        confirmation.SetActive(true);
    }

    private void HideConfirmation()
    {
        if (runner != null)
            runner.GetComponent<NetworkInputProvider>()?.ClearMenuInput();
        if (confirmation.activeSelf && runner != null && runner.IsRunning && runner.GetVisible())
            runner.ProvideInput = previousInput;
        confirmation.SetActive(false);
    }

    private void OnDestroy()
    {
        if (confirmation != null && confirmation.activeSelf && runner != null && runner.IsRunning && runner.GetVisible())
            runner.ProvideInput = previousInput;
    }

    private static RectTransform MakeRect(string name, Transform parent, Vector2 size, Vector2 position)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.layer = parent.gameObject.layer;
        RectTransform rect = (RectTransform)obj.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        return rect;
    }

    private static RectTransform MakeBackdrop(Transform parent, Color color)
    {
        RectTransform rect = MakeRect("Backdrop", parent, Vector2.zero, Vector2.zero);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        rect.gameObject.AddComponent<Image>().color = color;
        return rect;
    }

    private static TMP_Text MakeText(Transform parent, TMP_FontAsset font, string value, float size, Vector2 dimensions, Vector2 position)
    {
        TMP_Text text = MakeRect("Label", parent, dimensions, position).gameObject.AddComponent<TextMeshProUGUI>();
        text.font = font;
        text.text = value;
        text.fontSize = size;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        text.richText = false;
        return text;
    }

    private static Button MakeButton(Transform parent, TMP_FontAsset font, string label, Vector2 position, UnityEngine.Events.UnityAction action)
    {
        RectTransform rect = MakeRect(label, parent, new Vector2(270f, 52f), position);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = new Color(0.2f, 0.35f, 0.55f);
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(action);
        TMP_Text text = MakeText(rect, font, label, 21f, Vector2.zero, Vector2.zero);
        text.rectTransform.anchorMin = Vector2.zero;
        text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.offsetMin = text.rectTransform.offsetMax = Vector2.zero;
        return button;
    }
}
