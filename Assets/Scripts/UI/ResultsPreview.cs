using Fusion;
using NaughtyAttributes;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// Offline preview only. No runner is created and no real match data is changed.
public class ResultsPreview : MonoBehaviour
{
    [SerializeField] private bool previewTiedRanks;
    private NetworkResultsUI results;
    private TMP_Text styleSource;

    private void Start()
    {
        GameObject canvasObject = new GameObject("PreviewCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 900f);
        GameObject style = new GameObject("FontSource", typeof(RectTransform));
        style.transform.SetParent(canvasObject.transform, false);
        styleSource = style.AddComponent<TextMeshProUGUI>();
        styleSource.font = TMP_Settings.defaultFontAsset;
        styleSource.text = "";
        if (EventSystem.current == null)
        {
            GameObject events = new GameObject("PreviewEventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            events.transform.SetParent(transform, false);
            events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }
        RebuildPreview();
    }

    [Button("Rebuild Preview (Play Mode)")]
    public void RebuildPreview()
    {
        if (!Application.isPlaying || styleSource == null)
            return;
        if (results != null)
        {
            results.gameObject.SetActive(false);
            Destroy(results.gameObject);
        }
        string[] names = { "Nathanael", "Silver Player", "Another Challenger", "LongUsernamePreview", "Player Five" };
        MatchPlayerResult[] samples = new MatchPlayerResult[names.Length];
        for (int i = 0; i < samples.Length; i++)
        {
            int scoreIndex = previewTiedRanks && i == 2 ? 1 : i;
            samples[i] = new MatchPlayerResult
            {
                Player = PlayerRef.FromIndex(i),
                Username = names[i],
                Participated = true,
                Finished = true,
                TotalPoints = 15 - scoreIndex * 2,
                RoundPoints = 5 - i,
                TotalDuration = 150f - scoreIndex * 12f,
                TotalDamage = 900f - i * 125f,
                TotalKills = 5 - i
            };
        }
        results = NetworkResultsUI.CreatePreview(styleSource, samples);
    }
}
