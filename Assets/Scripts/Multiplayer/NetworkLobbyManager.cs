using System;
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum LobbyStartError
{
    None,
    SceneMissing,
    SceneLoadFailed
}

[RequireComponent(typeof(NetworkObject))]
public class NetworkLobbyManager : NetworkBehaviour
{
    [SerializeField, Min(2)] private int minimumPlayers = 2;
    [SerializeField] private string multiplayerGameScenePath = "Assets/Scenes/MultiplayerGame.unity";
    [Networked] public PlayerRef Leader { get; private set; }
    [Networked] public int ConnectedPlayers { get; private set; }
    [Networked] public int ReadyPlayers { get; private set; }
    [Networked] public NetworkBool StartingGame { get; private set; }
    [Networked] public LobbyStartError StartError { get; private set; }
    private double nextStartRequestTime;
    public int MinimumPlayers => minimumPlayers;
    public bool IsLocalLeader => Object != null && Object.IsValid &&
        Runner.LocalPlayer != PlayerRef.None && Runner.LocalPlayer == Leader;
    public bool CanRequestStart => IsLocalLeader && !StartingGame &&
        ReadyPlayers >= minimumPlayers && ReadyPlayers == ConnectedPlayers;

    public override void Spawned()
    {
        nextStartRequestTime = 0d;
        if (!Object.HasStateAuthority)
            return;
        Leader = PlayerRef.None;
        StartingGame = false;
        StartError = LobbyStartError.None;
        RefreshPlayers();
    }

    public void NotifyRoomClosing()
    {
        if (Object.HasStateAuthority)
            RPC_RoomClosing();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All, Channel = RpcChannel.Reliable)]
    private void RPC_RoomClosing()
    {
        if (!Runner.IsServer)
            NetworkSessionBootstrap.Instance?.NoticeRoomClosed();
    }

    public override void FixedUpdateNetwork()
    {
        if (Object.HasStateAuthority && !StartingGame)
            RefreshPlayers();
    }

    private void RefreshPlayers()
    {
        int connected = 0, ready = 0;
        bool leaderConnected = false;
        PlayerRef candidate = PlayerRef.None;
        foreach (PlayerRef player in Runner.ActivePlayers)
        {
            if (player == PlayerRef.None)
                continue;
            connected++;
            if (player == Leader)
                leaderConnected = true;
            if (candidate == PlayerRef.None || player.AsIndex < candidate.AsIndex)
                candidate = player;
            if (Runner.TryGetPlayerObject(player, out NetworkObject playerObject) &&
                playerObject != null && playerObject.IsValid)
                ready++;
        }
        ConnectedPlayers = connected;
        ReadyPlayers = ready;
        // Preserve the current leader; only choose another when the leader leaves.
        if (!leaderConnected)
            Leader = candidate;
    }

    public bool RequestStartGame()
    {
        if (!CanRequestStart || Time.unscaledTimeAsDouble < nextStartRequestTime)
            return false;
        nextStartRequestTime = Time.unscaledTimeAsDouble + 1d;
        RPC_RequestStartGame();
        return true;
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
    private void RPC_RequestStartGame(RpcInfo info = default)
    {
        if (!Object.HasStateAuthority || StartingGame || info.Source == PlayerRef.None)
            return;
        // Never trust the button's visibility: validate the actual RPC sender on the server.
        RefreshPlayers();
        if (info.Source != Leader || ReadyPlayers < minimumPlayers || ReadyPlayers != ConnectedPlayers)
            return;
        SceneRef gameScene = Runner.GetSceneRef(multiplayerGameScenePath);
        if (!gameScene.IsValid)
        {
            StartError = LobbyStartError.SceneMissing;
            return;
        }

        StartingGame = true;
        StartError = LobbyStartError.None;
        LoadGame(gameScene);
    }

    private async void LoadGame(SceneRef gameScene)
    {
        bool wasOpen = Runner.SessionInfo.IsOpen;
        try
        {
            // Do not admit new players in the middle of the lobby-to-game transition.
            Runner.SessionInfo.IsOpen = false;
            await Runner.LoadScene(gameScene, LoadSceneMode.Single);
        }
        catch (Exception exception)
        {
            if (this == null || Object == null || !Object.IsValid)
                return;
            StartingGame = false;
            StartError = LobbyStartError.SceneLoadFailed;
            Runner.SessionInfo.IsOpen = wasOpen;
            Debug.LogError($"Unable to load multiplayer game: {exception.Message}", this);
        }
    }
}
