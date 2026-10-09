using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum NetworkMatchState : byte
{
    WaitingForPlayers,
    Countdown,
    Playing,
    MatchOver
}

[RequireComponent(typeof(NetworkObject))]
public class NetworkMatchManager : NetworkBehaviour
{
    [SerializeField, Min(2)] private int minimumPlayers = 2;
    [Networked, OnChangedRender(nameof(HandleStateChanged))]
    public NetworkMatchState State { get; private set; }
    [Networked] public PlayerRef Winner { get; private set; }
    [Networked] public PlayerRef Leader { get; private set; }
    [Networked] public NetworkBool Restarting { get; private set; }
    private double nextRestartRequestTime;
    public bool IsLocalLeader => Object != null && Object.IsValid &&
        Runner.LocalPlayer != PlayerRef.None && Runner.LocalPlayer == Leader;
    [SerializeField, Min(0f)] private float countdownDuration = 3f;
    [Networked] public TickTimer CountdownTimer { get; private set; }
    [SerializeField, Min(1f)] private float matchDuration = 90f;
    [Networked] public TickTimer MatchTimer { get; private set; }
    public int CountdownNumber
    {
        get
        {
            if (State != NetworkMatchState.Countdown)
                return 0;
            float remaining =
                CountdownTimer.RemainingTime(Runner) ?? 0f;
            return Mathf.Max(1, Mathf.CeilToInt(remaining));
        }
    }
    public float MatchSecondsRemaining
    {
        get
        {
            if (State != NetworkMatchState.Playing)
                return 0f;
            float remaining = MatchTimer.RemainingTime(Runner) ?? 0f;
            return Mathf.Max(0f, remaining);
        }
    }

    public override void Spawned()
    {
        if (!Object.HasStateAuthority)
            return;
        Winner = PlayerRef.None;
        Leader = PlayerRef.None;
        Restarting = false;
        RefreshLeader();
        State = NetworkMatchState.WaitingForPlayers;
        CountdownTimer = TickTimer.None;
        MatchTimer = TickTimer.None;
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority)
            return;

        RefreshLeader();

        if (State == NetworkMatchState.WaitingForPlayers)
            TryBeginCountdown();
        else if (State == NetworkMatchState.Countdown)
            UpdateCountdown();
        else if (State == NetworkMatchState.Playing)
        {
            bool endedFromPlayerElim = TryEndMatchFromElimination();
            if (!endedFromPlayerElim && MatchTimer.Expired(Runner))
                EndMatch(PlayerRef.None);
        }
    }

    private void TryBeginCountdown()
    {
        if (CountReadyPlayers() < minimumPlayers)
            return;
        CountdownTimer = TickTimer.CreateFromSeconds(Runner, countdownDuration);
        State = NetworkMatchState.Countdown;
    }

    private void UpdateCountdown()
    {
        if (CountReadyPlayers() < minimumPlayers)
        {
            CountdownTimer = TickTimer.None;
            State = NetworkMatchState.WaitingForPlayers;
            return;
        }
        if (CountdownTimer.Expired(Runner))
            BeginPlaying();
    }

    private void BeginPlaying()
    {
        CountdownTimer = TickTimer.None;
        MatchTimer = TickTimer.CreateFromSeconds(Runner, matchDuration);
        State = NetworkMatchState.Playing;
    }

    private void EndMatch(PlayerRef winner)
    {
        Winner = winner;
        MatchTimer = TickTimer.None;
        State = NetworkMatchState.MatchOver;
    }

    private bool TryEndMatchFromElimination()
    {
        int livingPlayers = 0;
        PlayerRef lastLivingPlayer = PlayerRef.None;
        foreach (PlayerRef player in Runner.ActivePlayers)
        {
            if (!Runner.TryGetPlayerObject(
                    player, out NetworkObject playerObject))
                continue;
            if (playerObject == null)
                continue;

            NetworkHealth health = playerObject.GetComponent<NetworkHealth>();
            if (health == null || health.IsDead)
                continue;
            livingPlayers++;
            lastLivingPlayer = player;
        }
        if (livingPlayers > 1)
            return false;

        PlayerRef winner = livingPlayers == 1 ? lastLivingPlayer : PlayerRef.None;
        EndMatch(winner);

        return true;
    }

    public void RestartMatch()
    {
        if (!IsLocalLeader || State != NetworkMatchState.MatchOver || Restarting ||
            Time.unscaledTimeAsDouble < nextRestartRequestTime)
            return;
        nextRestartRequestTime = Time.unscaledTimeAsDouble + 1d;
        RPC_RequestRestart();
    }

    private void RefreshLeader()
    {
        bool leaderConnected = false;
        PlayerRef candidate = PlayerRef.None;
        // Same leader-selection rule as the lobby; dead players retain their restart permission.
        foreach (PlayerRef player in Runner.ActivePlayers)
        {
            if (player == PlayerRef.None)
                continue;
            if (player == Leader)
                leaderConnected = true;
            if (candidate == PlayerRef.None || player.AsIndex < candidate.AsIndex)
                candidate = player;
        }
        if (!leaderConnected)
            Leader = candidate;
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
    private void RPC_RequestRestart(RpcInfo info = default)
    {
        if (!Object.HasStateAuthority || State != NetworkMatchState.MatchOver || Restarting || info.Source == PlayerRef.None)
            return;
        RefreshLeader();
        if (info.Source != Leader)
            return;

        SceneRef currentScene = Runner.GetSceneRef(gameObject);
        if (!currentScene.IsValid)
        {
            Debug.LogError("Unable to find the current multiplayer scene.", this);
            return;
        }
        Restarting = true;
        ReloadMatch(currentScene);
    }

    private async void ReloadMatch(SceneRef scene)
    {
        try
        {
            await Runner.LoadScene(scene, LoadSceneMode.Single);
        }
        catch (System.Exception exception)
        {
            if (this == null || Object == null || !Object.IsValid)
                return;
            Restarting = false;
            Debug.LogError($"Unable to restart multiplayer match: {exception.Message}", this);
        }
    }

    private void HandleStateChanged()
    {
        if (State == NetworkMatchState.Playing)
        {
            Debug.Log("Network match started.", this);
            return;
        }
        if (State != NetworkMatchState.MatchOver)
            return;

        if (Winner == PlayerRef.None)
            Debug.Log("Network match ended in a draw.", this);
        else
            Debug.Log($"Network match over. Winner: {Winner}", this);
    }

    private int CountReadyPlayers()
    {
        int readyPlayers = 0;
        foreach (PlayerRef player in Runner.ActivePlayers)
        {
            if (!Runner.TryGetPlayerObject(player, out NetworkObject playerObject))
                continue;

            if (playerObject != null && playerObject.GetComponent<NetworkHealth>() != null)
                readyPlayers++;
        }
        return readyPlayers;
    }
}
