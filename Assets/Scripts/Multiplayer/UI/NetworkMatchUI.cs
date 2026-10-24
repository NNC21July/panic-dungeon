using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(NetworkMatchManager))]
public class NetworkMatchUI : NetworkBehaviour
{
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private GameObject roundOverPanel;
    [SerializeField] private GameObject darkOverlay;
    [SerializeField] private Button restartButton;
    [SerializeField] private TMP_Text countdownText;
    [SerializeField] private TMP_Text timerText;
    private NetworkMatchManager matchManager;
    private NetworkPerformanceUI performanceUI;
    private NetworkCharacterSelectionUI selectionUI;
    private NetworkSessionUI sessionUI;
    private NetworkSpectatorUI spectatorUI;

    private void Awake()
    {
        SerializedFieldValidator.Validate(this);
        matchManager = GetComponent<NetworkMatchManager>();
        resultText.richText = false;
        resultText.enableAutoSizing = true;
        resultText.fontSizeMax = resultText.fontSize;
        resultText.fontSizeMin = Mathf.Min(18f, resultText.fontSize);
    }

    public override void Spawned()
    {
        if (!Runner.IsServer)
        {
            performanceUI = NetworkPerformanceUI.Create(Runner, timerText);
            selectionUI = NetworkCharacterSelectionUI.Create(matchManager, timerText);
            sessionUI = NetworkSessionUI.Create(Runner, timerText);
            spectatorUI = NetworkSpectatorUI.Create(matchManager, timerText);
        }
        roundOverPanel.SetActive(false);
        darkOverlay.SetActive(false);
        restartButton.gameObject.SetActive(false);
        countdownText.gameObject.SetActive(false);
        timerText.gameObject.SetActive(false);
    }

    public override void Render()
    {
        bool isCountingDown = matchManager.State == NetworkMatchState.Countdown;
        SetActiveIfChanged(countdownText.gameObject, isCountingDown);
        if (isCountingDown)
            countdownText.text = matchManager.CountdownNumber.ToString();

        // Retained as the font/style source for runtime UI, never displayed as a match timer.
        SetActiveIfChanged(timerText.gameObject, false);

        bool matchOver = matchManager.State == NetworkMatchState.MatchOver;
        SetActiveIfChanged(roundOverPanel, matchOver);
        SetActiveIfChanged(darkOverlay, matchOver);

        if (!matchOver)
            return;

        if (matchManager.Winner == PlayerRef.None)
            resultText.text = "Draw!";
        else
            resultText.text = $"Winner is: {matchManager.WinnerName}";

        SetActiveIfChanged(restartButton.gameObject, matchManager.IsLocalLeader);
        restartButton.interactable = !matchManager.Restarting;
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (spectatorUI != null)
            Destroy(spectatorUI.gameObject);
        if (sessionUI != null)
            Destroy(sessionUI.gameObject);
        if (performanceUI != null)
            Destroy(performanceUI.gameObject);
        if (selectionUI != null)
            Destroy(selectionUI.gameObject);
    }

    private static void SetActiveIfChanged(GameObject target, bool active)
    {
        if (target.activeSelf != active)
            target.SetActive(active);
    }
}
