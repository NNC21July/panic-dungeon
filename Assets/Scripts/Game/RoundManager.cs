using System.Collections;
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
    private RoundState curState;
    private float countdownTimer, roundTimer;
    private string roundResult = "";
    public RoundState CurState => curState;
    public float CountdownTimer => countdownTimer;
    public float RoundTimer => roundTimer;
    public string RoundResult => roundResult;

    private void Awake()
    {
        SerializedFieldValidator.Validate(this);
        playerHealth.OnDeath += End;
    }

    private void Update()
    {
        if (curState == RoundState.Countdown)
        {
            countdownTimer -= Time.deltaTime;

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
        playerHealth.SetDamageEnabled(false);
        roundResult = "";
    }

    private void Play()
    {
        curState = RoundState.Playing;
        roundTimer = roundDuration;
        trapController.StartTraps();
        playerHealth.SetDamageEnabled(true);
    }

    private void End(DamageInfo damageInfo) // end by player death
    {
        roundResult = "Player dead";
        EndRound();
    }

    private void End() // end by round timer finish
    {
        roundResult = "Player won";
        EndRound();
    }

    private void EndRound()
    {
        if (curState != RoundState.Playing)
            return;
        curState = RoundState.RoundOver;
        trapController.StopTraps();
        playerHealth.SetDamageEnabled(false);
    }

    private void OnDestroy()
    {
        playerHealth.OnDeath -= End;
    }
}