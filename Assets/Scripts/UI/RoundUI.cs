using TMPro;
using UnityEngine;

public class RoundUI : MonoBehaviour
{
    [SerializeField] private RoundManager roundManager;
    [SerializeField] private TMP_Text countdown, timer, result;

    private void Awake()
    {
        SerializedFieldValidator.Validate(this);
    }

    private void Update()
    {
        countdown.gameObject.SetActive(false);
        timer.gameObject.SetActive(false);
        result.gameObject.SetActive(false);

        switch (roundManager.CurState)
        {
            case RoundState.Waiting:
                result.gameObject.SetActive(true);
                result.text = "Press E to start";
                break;
            case RoundState.Countdown:
                countdown.gameObject.SetActive(true);
                countdown.text = Mathf.CeilToInt(roundManager.CountdownTimer).ToString();
                break;
            case RoundState.Playing:
                timer.gameObject.SetActive(true);
                timer.text = roundManager.RoundTimer.ToString("F1");
                break;
            case RoundState.RoundOver:
                result.gameObject.SetActive(true);
                result.text = roundManager.RoundResult;
                break;
        }
    }
}