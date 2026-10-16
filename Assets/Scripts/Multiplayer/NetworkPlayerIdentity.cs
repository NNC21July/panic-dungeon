using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NetworkPlayerIdentity : NetworkBehaviour
{
    [Networked] public NetworkString<_16> Username { get; private set; }
    [SerializeField] private float labelHeight = 1.05f;
    [SerializeField] private float labelScale = 0.008f;
    [SerializeField] private Vector2 labelPadding = new Vector2(8f, 3f);
    [SerializeField] private float smoothTime = 0.08f;
    private CanvasGroup labelGroup;
    private TMP_Text label;
    private RectTransform labelRoot;
    private NetworkHealth health;
    private Vector3 labelPos, labelVelocity = Vector3.zero;
    public string DisplayName => Username.Length > 0 ? Username.ToString() : $"Player {Object.InputAuthority.AsIndex + 1}";

    public override void Spawned()
    {
        health = GetComponent<NetworkHealth>();
        if (Object.HasInputAuthority)
        {
            string name = PlayerDisplayName.Saved;
            RPC_SetUsername(string.IsNullOrEmpty(name) ? $"Player {Runner.LocalPlayer.AsIndex + 1}" : name);
        }
        if (!Runner.IsServer && !Object.HasInputAuthority)
            BuildLabel();
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_SetUsername(NetworkString<_16> requestedName)
    {
        if (Username.Length > 0)
            return;
        string name = PlayerDisplayName.Clean(requestedName.ToString());
        if (!string.IsNullOrEmpty(name))
            Username = name;
    }

    private void BuildLabel()
    {
        GameObject root = new GameObject("PlayerName", typeof(RectTransform));
        root.layer = gameObject.layer;
        labelRoot = (RectTransform)root.transform;
        labelRoot.SetParent(transform, false);
        labelRoot.localPosition = Vector3.up * labelHeight;
        labelRoot.localScale = Vector3.one * labelScale;
        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 20;
        labelGroup = root.AddComponent<CanvasGroup>();
        labelGroup.blocksRaycasts = false;
        labelGroup.interactable = false;
        Image background = root.AddComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 0.5f);
        background.raycastTarget = false;
        GameObject text = new GameObject("Username", typeof(RectTransform));
        text.layer = gameObject.layer;
        text.transform.SetParent(labelRoot, false);
        label = text.AddComponent<TextMeshProUGUI>();
        label.font = TMP_Settings.defaultFontAsset;
        label.fontSize = 24f;
        label.enableAutoSizing = false;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.richText = false;
        label.raycastTarget = false;
        label.rectTransform.anchorMin = Vector2.zero;
        label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.offsetMin = labelPadding;
        label.rectTransform.offsetMax = -labelPadding;
        labelPos = labelRoot.position;
        UpdateLabel();
        CornerBorderUI.Create(labelRoot, labelRoot.sizeDelta, 10f, 1.5f);
    }

    private void LateUpdate()
    {
        if (label != null && Object != null && Object.IsValid)
        {
            UpdateLabel();

            Vector3 targetPos = transform.TransformPoint(Vector3.up * labelHeight);
            labelPos = Vector3.SmoothDamp(labelPos, targetPos, ref labelVelocity, smoothTime);
            labelRoot.position = labelPos;
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (labelRoot != null)
            Destroy(labelRoot.gameObject);
        label = null;
        labelRoot = null;
        labelGroup = null;
    }

    private void UpdateLabel()
    {
        string displayName = DisplayName;
        if (label.text != displayName)
        {
            label.text = displayName;
            // Measure at the chosen font size, then wrap the panel around the text.
            labelRoot.sizeDelta = label.GetPreferredValues(displayName, Mathf.Infinity, Mathf.Infinity) + labelPadding * 2f;
        }
        labelRoot.rotation = Quaternion.identity;
        labelGroup.alpha = Runner.GetVisible() && !Object.HasInputAuthority && (health == null || !health.IsDead) ? 1f : 0f;
    }
}
