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

    private void Awake()
    {
        playerHealth.OnDeath += End;
    }

    private void Update()
    {
        if (curState == RoundState.Countdown)
        {
            if (countdownTimer - Time.deltaTime < (int)countdownTimer && countdownTimer >= (int)countdownTimer)
                Debug.Log((int)countdownTimer); // prints countdown in whole number
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
    }

    private void Play()
    {
        curState = RoundState.Playing;
        roundTimer = roundDuration;
        trapController.StartTraps();
    }

    private void End(DamageInfo damageInfo) // end by player death
    {
        if (curState != RoundState.Playing)
            return;
        curState = RoundState.RoundOver;
        trapController.StopTraps();
        Debug.Log("Player dead");
    }

    private void End() // end by round timer finish
    {
        if (curState != RoundState.Playing)
            return;
        curState = RoundState.RoundOver;
        trapController.StopTraps();
        Debug.Log("Player won");
    }

    private void OnDestroy()
    {
        playerHealth.OnDeath -= End;
    }
}