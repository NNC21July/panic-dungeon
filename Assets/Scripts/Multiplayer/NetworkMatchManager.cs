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
    [Networked, Capacity(10)] public NetworkArray<PlayerRef> ColorOwners => default;
    [Networked] private int readyColors { get; set; }
    public string SelectionMessage { get; private set; } = "Choose a color, then press Ready.";
    private double nextSelectionRequestTime;
    private double nextRestartRequestTime;
    public int MinimumPlayers => minimumPlayers;
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
        SelectionMessage = "Choose a color, then press Ready.";
        nextSelectionRequestTime = 0d;
        nextRestartRequestTime = 0d;
        if (!Object.HasStateAuthority)
            return;
        Winner = PlayerRef.None;
        Leader = PlayerRef.None;
        Restarting = false;
        readyColors = 0;
        for (int i = 0; i < CharacterColors.Count; i++)
            ColorOwners.Set(i, PlayerRef.None);
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

        ReleaseDisconnectedColors();
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
        if (!AllPlayersSelected())
            return;
        CountdownTimer = TickTimer.CreateFromSeconds(Runner, countdownDuration);
        State = NetworkMatchState.Countdown;
    }

    private void UpdateCountdown()
    {
        if (!AllPlayersSelected())
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

    public override void Render()
    {
        for (int i = 0; i < CharacterColors.Count; i++)
        {
            PlayerRef player = ColorOwners[i];
            if (player == PlayerRef.None || !Runner.TryGetPlayerObject(player, out NetworkObject obj) || obj == null)
                continue;
            DamageFlash flash = obj.GetComponent<DamageFlash>();
            if (flash != null)
                flash.SetBaseColor(CharacterColors.Get(i));
        }
    }

    public int ConnectedPlayerCount
    {
        get
        {
            int count = 0;
            foreach (PlayerRef player in Runner.ActivePlayers)
                if (player != PlayerRef.None)
                    count++;
            return count;
        }
    }

    public int ReadyPlayerCount
    {
        get
        {
            int count = 0;
            for (int i = 0; i < CharacterColors.Count; i++)
                if (ColorOwners[i] != PlayerRef.None && IsColorReady(i))
                    count++;
            return count;
        }
    }

    public int FindColor(PlayerRef player)
    {
        if (player != PlayerRef.None)
            for (int i = 0; i < CharacterColors.Count; i++)
                if (ColorOwners[i] == player)
                    return i;
        return -1;
    }

    public bool IsColorReady(int index) => index >= 0 && index < CharacterColors.Count && (readyColors & (1 << index)) != 0;

    private bool AllPlayersSelected()
    {
        if (ConnectedPlayerCount < minimumPlayers || ReadyPlayerCount != ConnectedPlayerCount)
            return false;
        foreach (PlayerRef player in Runner.ActivePlayers)
        {
            if (player == PlayerRef.None)
                continue;
            if (!Runner.TryGetPlayerObject(player, out NetworkObject obj) || obj == null || !obj.IsValid ||
                obj.GetComponent<NetworkHealth>() == null || !IsColorReady(FindColor(player)))
                return false;
        }
        return true;
    }

    private void ReleaseDisconnectedColors()
    {
        for (int i = 0; i < CharacterColors.Count; i++)
        {
            if (ColorOwners[i] == PlayerRef.None)
                continue;
            bool connected = false;
            foreach (PlayerRef player in Runner.ActivePlayers)
                if (player == ColorOwners[i])
                    connected = true;
            if (connected)
                continue;
            ColorOwners.Set(i, PlayerRef.None);
            readyColors &= ~(1 << i);
        }
    }

    public void RequestColor(int index)
    {
        if (!CanSendSelection())
            return;
        SelectionMessage = "Reserving color...";
        RPC_SelectColor(index);
    }

    public void RequestReady(bool ready)
    {
        if (!CanSendSelection())
            return;
        SelectionMessage = "Updating ready status...";
        RPC_SetReady(ready);
    }

    private bool CanSendSelection()
    {
        if (Object == null || !Object.IsValid || State != NetworkMatchState.WaitingForPlayers ||
            Runner.LocalPlayer == PlayerRef.None || Time.unscaledTimeAsDouble < nextSelectionRequestTime)
            return false;
        nextSelectionRequestTime = Time.unscaledTimeAsDouble + 0.15d;
        return true;
    }

    private bool CanSelect(PlayerRef player)
    {
        if (!Object.HasStateAuthority || State != NetworkMatchState.WaitingForPlayers || player == PlayerRef.None)
            return false;
        foreach (PlayerRef active in Runner.ActivePlayers)
            if (active == player)
                return Runner.TryGetPlayerObject(player, out NetworkObject obj) && obj != null && obj.IsValid;
        return false;
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
    private void RPC_SelectColor(int index, RpcInfo info = default)
    {
        if (!CanSelect(info.Source) || index < 0 || index >= CharacterColors.Count)
            return;
        ReleaseDisconnectedColors();
        int previous = FindColor(info.Source);
        if (IsColorReady(previous))
        {
            RPC_SelectionReply(info.Source, "Press Change Color before picking again.");
            return;
        }
        if (ColorOwners[index] != PlayerRef.None && ColorOwners[index] != info.Source)
        {
            RPC_SelectionReply(info.Source, "That color was just picked. Choose another.");
            return;
        }
        if (previous >= 0)
        {
            ColorOwners.Set(previous, PlayerRef.None);
            readyColors &= ~(1 << previous);
        }
        ColorOwners.Set(index, info.Source);
        RPC_SelectionReply(info.Source, "Color selected. Press Ready to confirm.");
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
    private void RPC_SetReady(bool ready, RpcInfo info = default)
    {
        if (!CanSelect(info.Source))
            return;
        int index = FindColor(info.Source);
        if (index < 0)
            return;
        if (ready)
            readyColors |= 1 << index;
        else
            readyColors &= ~(1 << index);
        RPC_SelectionReply(info.Source, ready ? "Ready! Waiting for the other players..." : "Choose a color, then press Ready.");
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All, Channel = RpcChannel.Reliable)]
    private void RPC_SelectionReply(PlayerRef player, string message)
    {
        if (Runner.LocalPlayer == player)
            SelectionMessage = message;
    }
}
