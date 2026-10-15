using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;

// Own the player-facing session lifecycle without changing Fusion's development bootstrap.
public class NetworkSessionBootstrap : FusionBootstrap
{
    public static NetworkSessionBootstrap Instance { get; private set; }
    public static string MenuMessage { get; private set; } = "";
    private static bool returningToMenu;
    private static bool menuConnectionPending;
    public static bool ReturnToMainMenu { get; private set; }
    public static bool MenuCreating { get; private set; }
    public static string MenuRoomCode { get; private set; } = "";

    public static void QueueMenuConnection(string code, bool creating)
    {
        MenuRoomCode = code;
        MenuCreating = creating;
        ReturnToMainMenu = true;
        menuConnectionPending = true;
        MenuMessage = "";
    }
    private readonly List<NetworkRunner> sessionRunners = new();
    private bool returning, quitting;
    private string pendingMessage;
    private double startupDeadline;
    public bool IsReturning => returning;
    public bool HasLocalServer
    {
        get
        {
            foreach (NetworkRunner runner in sessionRunners)
                if (runner != null && runner.IsRunning && runner.IsServer)
                    return true;
            return false;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSessionStatics()
    {
        Instance = null;
        MenuMessage = "";
        returningToMenu = false;
        menuConnectionPending = false;
        ReturnToMainMenu = false;
        MenuCreating = false;
        MenuRoomCode = "";
    }

    protected override void Start()
    {
        // Multi-peer scene copies contain a bootstrap too; only the original owns startup.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        bool startFromMenu = menuConnectionPending;
        menuConnectionPending = false;
        if (returningToMenu || startFromMenu || !Application.isEditor)
            StartMode = StartModes.Manual;
        returningToMenu = false;
        base.Start();
        if (startFromMenu)
        {
            DefaultRoomName = MenuRoomCode;
            BeginConnection();
            if (MenuCreating)
                StartServerPlusClients(1);
            else
                StartClient();
        }
    }

    public void BeginConnection()
    {
        MenuMessage = "";
        startupDeadline = Time.unscaledTimeAsDouble + 30d;
    }

    protected override async Task InitializeNetworkRunner(NetworkRunner runner, GameMode gameMode,
        NetAddress address, SceneRef scene, Action<NetworkRunner> onGameStarted, INetworkRunnerUpdater updater = null)
    {
        sessionRunners.Add(runner);
        if (startupDeadline == 0d)
            BeginConnection();
        NetworkDelegates callbacks = new NetworkDelegates();
        callbacks.OnShutdown = (stopped, reason) =>
        {
            if (!returning && !quitting && string.IsNullOrEmpty(pendingMessage))
                pendingMessage = ShutdownMessage(reason);
        };
        callbacks.OnDisconnectedFromServer = (disconnected, reason) =>
        {
            if (!returning && !quitting && string.IsNullOrEmpty(pendingMessage))
                pendingMessage = "Connection to the server was lost. The creator may have left. You can try joining again.";
        };
        runner.AddCallbacks(callbacks);
        try
        {
            Task startup = base.InitializeNetworkRunner(runner, gameMode, address, scene, onGameStarted, updater);
            await startup;
            if (startup is Task<StartGameResult> result && !result.Result.Ok && !returning)
                pendingMessage = ShutdownMessage(result.Result.ShutdownReason);
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"Session startup failed: {exception.Message}", this);
            if (!returning)
                pendingMessage = "Unable to connect. Check your connection and room code, then try again.";
        }
    }

    private void Update()
    {
        if (Instance != this || returning || quitting)
            return;
        // Shutdown callbacks run inside Fusion; defer cleanup until they have returned.
        if (!string.IsNullOrEmpty(pendingMessage))
        {
            ReturnToMenu(pendingMessage);
            return;
        }
        if (startupDeadline == 0d)
            return;
        foreach (NetworkRunner runner in sessionRunners)
            if (runner != null && runner.IsRunning && !runner.IsServer && runner.IsConnectedToServer)
            {
                startupDeadline = 0d;
                return;
            }
        if (Time.unscaledTimeAsDouble >= startupDeadline)
            ReturnToMenu("Connection timed out. Check the room code and try again.");
    }

    public void LeaveSession()
    {
        if (returning)
            return;
        ReturnToMenu(HasLocalServer ? "Room closed. All players have been disconnected." : "You left the room.", HasLocalServer);
    }

    public void NoticeRoomClosed()
    {
        if (!returning && !quitting)
            pendingMessage = "The room creator closed the session.";
    }

    private async void ReturnToMenu(string message, bool notifyClients = false)
    {
        returning = true;
        MenuMessage = message;
        returningToMenu = true;
        StopAllCoroutines();
        // The lobby no longer owns Create/Join controls, including for editor-started sessions.
        if (!ReturnToMainMenu)
        {
            MenuRoomCode = DefaultRoomName;
            MenuCreating = HasLocalServer;
        }
        ReturnToMainMenu = true;
        string entryScene = "Assets/Scenes/MainMenu.unity";
        if (notifyClients)
        {
            foreach (NetworkLobbyManager lobby in FindObjectsByType<NetworkLobbyManager>())
                if (lobby.Object != null && lobby.Object.IsValid && lobby.Object.HasStateAuthority && sessionRunners.Contains(lobby.Runner))
                    lobby.NotifyRoomClosing();
            foreach (NetworkMatchManager match in FindObjectsByType<NetworkMatchManager>())
                if (match.Object != null && match.Object.IsValid && match.Object.HasStateAuthority && sessionRunners.Contains(match.Runner))
                    match.NotifyRoomClosing();
            // Best-effort graceful notice; abrupt disconnects use an honest generic message.
            await Task.Delay(250);
        }
        if (quitting || this == null)
            return;
        foreach (NetworkRunner runner in sessionRunners.ToArray())
        {
            if (runner == null)
                continue;
            try
            {
                await runner.Shutdown();
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Session cleanup failed: {exception.Message}");
                if (runner != null)
                    Destroy(runner.gameObject);
            }
        }
        if (quitting || this == null)
            return;
        // StartWithClients clones this template into DontDestroyOnLoad.
        if (RunnerPrefab != null)
            Destroy(RunnerPrefab.gameObject);
        Instance = null;
        await SceneManager.LoadSceneAsync(entryScene, LoadSceneMode.Single);
        if (this != null)
            Destroy(gameObject);
    }

    private static string ShutdownMessage(ShutdownReason reason)
    {
        switch (reason)
        {
            case ShutdownReason.GameIsFull: return "This room is full. Try a different room.";
            case ShutdownReason.GameNotFound: return "Room not found. Check the code and ask the creator to open the room first.";
            case ShutdownReason.GameClosed: return "This room has already started or is closed.";
            case ShutdownReason.GameIdAlreadyExists: return "That room code is already in use. Choose another code.";
            case ShutdownReason.InvalidAuthentication: return "Photon configuration could not be authenticated. Check your Fusion App ID.";
            default: return "Disconnected from the session. The creator may have left, or the connection was lost. You can try joining again.";
        }
    }

    private void OnApplicationQuit()
    {
        quitting = true;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
