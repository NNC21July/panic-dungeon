using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NetworkCharacterSelectionUI : MonoBehaviour
{
    private NetworkMatchManager manager;
    private RectTransform content;
    private CanvasGroup group;
    private TMP_Text status, counter, readyLabel;
    private Button readyButton;
    private readonly CharacterColorOption[] options = new CharacterColorOption[10];
    private float fade;

    public static NetworkCharacterSelectionUI Create(NetworkMatchManager manager, TMP_Text styleSource)
    {
        Canvas canvas = styleSource.GetComponentInParent<Canvas>().rootCanvas;
        RectTransform root = MakeRect("CharacterSelection", canvas.transform, Vector2.zero, Vector2.zero);
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = root.offsetMax = Vector2.zero;
        Image backdrop = root.gameObject.AddComponent<Image>();
        backdrop.color = new Color(0.015f, 0.02f, 0.035f, 0.94f);
        NetworkCharacterSelectionUI ui = root.gameObject.AddComponent<NetworkCharacterSelectionUI>();
        ui.manager = manager;
        ui.group = root.gameObject.AddComponent<CanvasGroup>();
        ui.group.alpha = 0f;
        ui.Build(styleSource.font);
        manager.Runner.AddVisibilityNodes(root.gameObject);
        return ui;
    }

    private void Build(TMP_FontAsset font)
    {
        content = MakeRect("SelectionContent", transform, new Vector2(760f, 520f), Vector2.zero);
        MakeText("Title", content, font, "CHOOSE YOUR COLOR", 40f, new Vector2(720f, 64f), new Vector2(0f, 215f));
        MakeText("Subtitle", content, font, "One color per player. Pick yours, then get ready.", 20f,
            new Vector2(700f, 38f), new Vector2(0f, 163f));
        for (int i = 0; i < options.Length; i++)
        {
            int index = i;
            Vector2 position = new Vector2((i % 5 - 2) * 136f, i < 5 ? 72f : -66f);
            RectTransform card = MakeRect("Color" + (i + 1), content, new Vector2(120f, 128f), position);
            Image hitArea = card.gameObject.AddComponent<Image>();
            hitArea.color = Color.clear;
            Button button = card.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => manager.RequestColor(index));
            CharacterColorOption option = card.gameObject.AddComponent<CharacterColorOption>();
            RectTransform circle = MakeRect("Circle", card, new Vector2(82f, 82f), new Vector2(0f, -8f));
            SelectionCircleGraphic graphic = circle.gameObject.AddComponent<SelectionCircleGraphic>();
            graphic.color = CharacterColors.Get(i);
            graphic.raycastTarget = false;
            circle.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            RectTransform shimmer = MakeRect("Shimmer", circle, new Vector2(22f, 180f), Vector2.zero);
            shimmer.localRotation = Quaternion.Euler(0f, 0f, -35f);
            SelectionShimmerGraphic shine = shimmer.gameObject.AddComponent<SelectionShimmerGraphic>();
            shine.color = new Color(1f, 1f, 1f, 0.8f);
            shine.raycastTarget = false;
            RectTransform border = MakeRect("SelectedCorners", card, new Vector2(104f, 104f), new Vector2(0f, -8f));
            CanvasGroup cornerGroup = border.gameObject.AddComponent<CanvasGroup>();
            cornerGroup.alpha = 0f;
            cornerGroup.blocksRaycasts = false;
            CornerBorderUI.Create(border, border.sizeDelta, 20f, 2f);
            TMP_Text label = MakeText("State", card, font, "", 16f, new Vector2(120f, 24f), new Vector2(0f, 55f));
            option.Configure(circle, graphic, shimmer, cornerGroup, label, button, CharacterColors.Get(i));
            options[i] = option;
        }
        counter = MakeText("ReadyCount", content, font, "0 / 0 READY", 22f, new Vector2(640f, 34f), new Vector2(0f, -157f));
        RectTransform readyRect = MakeRect("ReadyButton", content, new Vector2(270f, 52f), new Vector2(0f, -208f));
        Image readyImage = readyRect.gameObject.AddComponent<Image>();
        readyImage.color = new Color(0.2f, 0.35f, 0.55f);
        readyButton = readyRect.gameObject.AddComponent<Button>();
        readyButton.targetGraphic = readyImage;
        readyLabel = MakeText("Label", readyRect, font, "READY", 24f, new Vector2(260f, 50f), Vector2.zero);
        readyButton.onClick.AddListener(() =>
        {
            int index = manager.FindColor(manager.Runner.LocalPlayer);
            manager.RequestReady(!manager.IsColorReady(index));
        });
        status = MakeText("Status", content, font, "", 17f, new Vector2(720f, 36f), new Vector2(0f, -255f));
    }

    private void Update()
    {
        if (manager == null || manager.Object == null || !manager.Object.IsValid)
            return;
        bool selecting = manager.State == NetworkMatchState.WaitingForPlayers;
        bool visible = manager.Runner.GetVisible();
        fade = Mathf.MoveTowards(fade, selecting ? 1f : 0f, Time.unscaledDeltaTime * 6f);
        group.alpha = visible ? Mathf.SmoothStep(0f, 1f, fade) : 0f;
        group.blocksRaycasts = visible && selecting;
        group.interactable = visible && selecting;
        Rect canvasRect = ((RectTransform)transform).rect;
        content.localScale = Vector3.one * Mathf.Min(1f, canvasRect.width / 800f, canvasRect.height / 580f);
        if (!selecting)
            return;
        PlayerRef local = manager.Runner.LocalPlayer;
        int chosen = manager.FindColor(local);
        bool ready = manager.IsColorReady(chosen);
        for (int i = 0; i < options.Length; i++)
        {
            PlayerRef owner = manager.ColorOwners[i];
            options[i].SetState(owner == local && local != PlayerRef.None,
                owner != PlayerRef.None && owner != local, manager.IsColorReady(i), !ready);
        }
        readyButton.interactable = chosen >= 0;
        readyLabel.text = ready ? "CHANGE COLOR" : "READY";
        counter.text = $"{manager.ReadyPlayerCount} / {manager.ConnectedPlayerCount} READY";
        status.text = manager.ConnectedPlayerCount < manager.MinimumPlayers
            ? $"Waiting for at least {manager.MinimumPlayers} players..." : manager.SelectionMessage;
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

    private static TMP_Text MakeText(string name, Transform parent, TMP_FontAsset font, string value, float size,
        Vector2 dimensions, Vector2 position)
    {
        TMP_Text text = MakeRect(name, parent, dimensions, position).gameObject.AddComponent<TextMeshProUGUI>();
        text.font = font;
        text.text = value;
        text.fontSize = size;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        return text;
    }

}
