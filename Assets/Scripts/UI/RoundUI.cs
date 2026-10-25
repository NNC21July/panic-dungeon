using TMPro;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RoundUI : MonoBehaviour
{
    private const string MainMenuSceneName = "MainMenu";
    [SerializeField, Required] private RoundManager roundManager;
    [SerializeField, Required] private TMP_Text countdown, timer, result;
    [SerializeField, Required] private GameObject roundOverPanel, darkOverlay;

    private void Awake()
    {
        SerializedFieldValidator.Validate(this);
        roundOverPanel.SetActive(false);
        darkOverlay.SetActive(false);
    }

    private void Update()
    {
        RoundState state = roundManager.CurState;

        bool showCountdown = state == RoundState.Countdown;
        bool showTimer = state == RoundState.Playing;
        bool showRoundOver = state == RoundState.RoundOver;

        SetActiveIfChanged(countdown.gameObject, showCountdown);
        SetActiveIfChanged(timer.gameObject, showTimer);
        SetActiveIfChanged(result.gameObject, showRoundOver);
        SetActiveIfChanged(roundOverPanel, showRoundOver);
        SetActiveIfChanged(darkOverlay, showRoundOver);

        switch (state)
        {
            case RoundState.Countdown:
                countdown.text = roundManager.CountdownNum.ToString();
                break;
            case RoundState.Playing:
                timer.text = roundManager.RoundTimer.ToString("F1");
                break;
            case RoundState.RoundOver:
                result.text = roundManager.RoundResult;
                break;
        }
    }

    private static void SetActiveIfChanged(GameObject target, bool active)
    {
        if (target.activeSelf != active)
            target.SetActive(active);
    }

    public void RestartRound()
    {
        roundManager.Begin();
    }

    public void ReturnToMainMenu()
    {
        SceneManager.LoadScene(MainMenuSceneName);
    }
}
