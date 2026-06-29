using UnityEngine;

public enum RoundState
{
    Waiting, Countdown, Playing, RoundOver
}

public class RoundManager : MonoBehaviour
{
    [SerializeField] private TrapController trapController;
    [SerializeField] private Health playerHealth;
    [SerializeField] private PlayerReset playerReset;
    [SerializeField] private float countdownDuration = 3f, roundDuration = 60f;
    [SerializeField] private Transform playerSpawn;
    [SerializeField] private AudioClip roundStartSfx, victorySfx, defeatSfx, countdownBeepSfx;
    private RoundState curState;
    private float countdownTimer, roundTimer;
    private string roundResult = "";
    private int prevCountdownNum;
    public int CountdownNum => Mathf.CeilToInt(countdownTimer);
    public RoundState CurState => curState;
    public float RoundTimer => roundTimer;
    public string RoundResult => roundResult;

    private void Awake()
    {
        SerializedFieldValidator.Validate(this);
        playerHealth.OnDeath += End;
    }

    private void Start()
    {
        Begin();
    }

    private void Update()
    {
        if (curState == RoundState.Countdown)
        {
            countdownTimer = Mathf.Max(0f, countdownTimer - Time.deltaTime);
            int curNum = CountdownNum;
            if (curNum > 0 && curNum != prevCountdownNum)
            {
                prevCountdownNum = curNum;
                AudioManager.Instance?.PlaySfx(countdownBeepSfx, 0.4f);
            }

            if (countdownTimer <= 0f)
                Play();
        }
        else if (curState == RoundState.Playing)
        {
            roundTimer -= Time.deltaTime;
            if (roundTimer <= 0f)
                End();
        }
    }

    public void Begin()
    {
        if (curState != RoundState.Waiting && curState != RoundState.RoundOver)
            return;
        playerReset.ResetAt(playerSpawn.position);
        curState = RoundState.Countdown;
        countdownTimer = countdownDuration;
        prevCountdownNum = -1;
        playerHealth.SetDamageEnabled(false);
        roundResult = "";
    }

    private void Play()
    {
        curState = RoundState.Playing;
        roundTimer = roundDuration;
        trapController.StartTraps();
        playerHealth.SetDamageEnabled(true);
        AudioManager.Instance?.PlaySfx(roundStartSfx);
    }

    private void End(DamageInfo _) // end by player death
    {
        EndRound("Player dead!", defeatSfx);
    }

    private void End() // end by round timer finish
    {
        EndRound("Player won!", victorySfx);
    }

    private void EndRound(string result, AudioClip resultSfx)
    {
        if (curState != RoundState.Playing)
            return;
        curState = RoundState.RoundOver;
        trapController.StopTraps();
        playerHealth.SetDamageEnabled(false);
        roundResult = result;
        AudioManager.Instance?.PlaySfx(resultSfx, 0.4f);

    }

    private void OnDestroy()
    {
        playerHealth.OnDeath -= End;
    }
}