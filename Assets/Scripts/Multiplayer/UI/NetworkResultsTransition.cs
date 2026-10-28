using DG.Tweening;
using Fusion;
using UnityEngine;
using UnityEngine.UI;

// Lives on the runner, not the departing scene. Each peer has its own curtain.
public class NetworkResultsTransition : MonoBehaviour
{
    private NetworkRunner runner;
    private NetworkMatchManager departingMatch;
    private Canvas canvas;
    private RectTransform curtain;
    private CanvasGroup group;
    private ResultsPresentationSettings settings;
    private Tween motion;
    private float position = -1f, coveredAt;
    private bool active, covered, revealing, preview;

    public static NetworkResultsTransition Get(NetworkRunner runner, GameObject previewOwner = null)
    {
        GameObject owner = runner != null ? runner.gameObject : previewOwner;
        NetworkResultsTransition transition = owner.GetComponent<NetworkResultsTransition>();
        if (transition == null)
            transition = owner.AddComponent<NetworkResultsTransition>();
        transition.runner = runner;
        return transition;
    }

    public void Cover(NetworkMatchManager match, ResultsPresentationSettings settings)
    {
        if (active)
            return;
        this.settings = settings;
        departingMatch = match;
        preview = match == null;
        if (canvas == null)
        {
            GameObject root = new GameObject("ResultsSceneWipe", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster), typeof(CanvasGroup));
            root.transform.SetParent(transform, false);
            canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 32000;
            group = root.GetComponent<CanvasGroup>();
            GameObject rectangle = new GameObject("DarkGreyCurtain", typeof(RectTransform), typeof(Image));
            rectangle.transform.SetParent(root.transform, false);
            curtain = rectangle.GetComponent<RectTransform>();
            curtain.sizeDelta = Vector2.zero;
        }
        Color color = settings.wipeColor;
        color.a = 1f;
        curtain.GetComponent<Image>().color = color;
        active = true;
        covered = revealing = false;
        position = -1f;
        ApplyPosition();
        group.alpha = 1f;
        canvas.enabled = preview || (runner != null && runner.IsRunning && runner.GetVisible());
        group.blocksRaycasts = canvas.enabled;
        motion = DOTween.To(() => position, value => { position = value; ApplyPosition(); }, 0f,
            Mathf.Max(0.1f, settings.wipeDuration)).SetEase(settings.wipeEase).SetUpdate(true)
            .OnComplete(() => { covered = true; coveredAt = Time.unscaledTime; });
    }

    private void Update()
    {
        if (!active)
            return;
        bool visible = preview || (runner != null && runner.IsRunning && runner.GetVisible());
        canvas.enabled = visible;
        group.blocksRaycasts = visible;
        if (!preview && (runner == null || !runner.IsRunning))
        {
            Cancel();
            return;
        }
        if (!covered || revealing || Time.unscaledTime - coveredAt < 0.1f)
            return;
        bool matchValid = departingMatch != null && departingMatch.Object != null && departingMatch.Object.IsValid;
        bool failed = matchValid && departingMatch.TransitionFailed;
        bool loaded = !matchValid && runner != null && runner.SceneManager != null && !runner.SceneManager.IsBusy;
        if (preview || failed || loaded)
        {
            revealing = true;
            motion = DOTween.To(() => position, value => { position = value; ApplyPosition(); }, 1f,
                Mathf.Max(0.1f, settings.wipeDuration)).SetEase(settings.wipeEase).SetUpdate(true).OnComplete(Cancel);
        }
    }

    private void ApplyPosition()
    {
        curtain.anchorMin = new Vector2(position, 0f);
        curtain.anchorMax = new Vector2(position + 1f, 1f);
        curtain.offsetMin = curtain.offsetMax = Vector2.zero;
    }

    public void Cancel()
    {
        motion?.Kill();
        motion = null;
        active = covered = revealing = false;
        if (canvas != null)
        {
            canvas.enabled = false;
            group.alpha = 0f;
            group.blocksRaycasts = false;
        }
    }

    private void OnDisable() => Cancel();
    private void OnDestroy() => Cancel();
}
