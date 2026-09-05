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

    private void Awake()
    {
        SerializedFieldValidator.Validate(this);
        matchManager = GetComponent<NetworkMatchManager>();
    }

    public override void Spawned()
    {
        roundOverPanel.SetActive(false);
        darkOverlay.SetActive(false);
        restartButton.gameObject.SetActive(Object.HasStateAuthority);
        countdownText.gameObject.SetActive(false);
        timerText.gameObject.SetActive(false);
    }

    public override void Render()
    {
        bool isCountingDown = matchManager.State == NetworkMatchState.Countdown;
        SetActiveIfChanged(countdownText.gameObject, isCountingDown);
        if (isCountingDown)
            countdownText.text = matchManager.CountdownNumber.ToString();

        bool isPlaying = matchManager.State == NetworkMatchState.Playing;
        SetActiveIfChanged(timerText.gameObject, isPlaying);
        if (isPlaying)
            timerText.text = matchManager.MatchSecondsRemaining.ToString("F1");

        bool matchOver = matchManager.State == NetworkMatchState.MatchOver;
        SetActiveIfChanged(roundOverPanel, matchOver);
        SetActiveIfChanged(darkOverlay, matchOver);

        if (!matchOver)
            return;

        if (matchManager.Winner == PlayerRef.None)
            resultText.text = "Draw!";
        else if (matchManager.Winner == Runner.LocalPlayer)
            resultText.text = "You Win!";
        else
            resultText.text = "You Lose!";

        SetActiveIfChanged(restartButton.gameObject, Object.HasStateAuthority);
    }

    private static void SetActiveIfChanged(GameObject target, bool active)
    {
        if (target.activeSelf != active)
            target.SetActive(active);
    }
}