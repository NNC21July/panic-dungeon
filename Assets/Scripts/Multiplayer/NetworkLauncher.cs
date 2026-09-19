using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NetworkLauncher : MonoBehaviour
{
    [SerializeField] private FusionBootstrap bootstrap;
    [SerializeField] private TMP_InputField roomCodeInput;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Button hostButton, joinButton, startGameButton;
    [SerializeField] private NetworkLobbyManager lobby;
    private bool isStarting, assignedLocalView;
    private double connectionDeadline;

    private void Awake()
    {
        SerializedFieldValidator.Validate(this);
        // Automatic multi-peer startup is an editor test preset, not a player-build startup flow.
        if (!Application.isEditor)
            bootstrap.StartMode = FusionBootstrap.StartModes.Manual;
    }

    private void Update()
    {
        NetworkRunner runner = GetConnectedRunner();
        bool connected = runner != null;
        bool lobbyReady = connected && lobby != null && lobby.Object != null && lobby.Object.IsValid;
        bool leader = lobbyReady && lobby.IsLocalLeader;
        startGameButton.gameObject.SetActive(leader);
        startGameButton.interactable = leader && lobby.CanRequestStart;
        hostButton.gameObject.SetActive(!connected);
        joinButton.gameObject.SetActive(!connected);
        roomCodeInput.interactable = !connected && !isStarting;

        if (!connected)
        {
            if (isStarting && Time.unscaledTimeAsDouble >= connectionDeadline)
            {
                statusText.text = "Connection did not complete. Check the room code and Console, then restart the session.";
                isStarting = false;
            }
            return;
        }
        isStarting = false;
        if (!assignedLocalView && !runner.IsServer)
        {
            SelectInitialClientView(runner);
            assignedLocalView = true;
        }
        roomCodeInput.SetTextWithoutNotify(runner.SessionInfo.Name);
        if (!lobbyReady)
        {
            statusText.text = "Loading lobby...";
            return;
        }
        if (lobby.StartingGame)
            statusText.text = "Loading game...";
        else if (lobby.StartError != LobbyStartError.None)
            statusText.text = lobby.StartError == LobbyStartError.SceneMissing
                ? "MultiplayerGame is missing from the build scene list."
                : "Game failed to load. Check the server Console and try again.";
        else if (runner.IsServer)
            statusText.text = $"Server running - {lobby.ConnectedPlayers} clients; waiting for the lobby leader.";
        else if (leader)
            statusText.text = lobby.CanRequestStart
                ? $"You are the lobby leader - {lobby.ReadyPlayers} players ready."
                : $"You are the lobby leader - waiting for players ({lobby.ReadyPlayers}/{lobby.MinimumPlayers}).";
        else
            statusText.text = $"Joined room - {lobby.ReadyPlayers} players ready; waiting for the lobby leader.";
    }

    private bool TryPrepareConnection(bool hosting)
    {
        if (isStarting || GetConnectedRunner() != null || bootstrap == null ||
            bootstrap.CurrentStage != FusionBootstrap.Stage.Disconnected)
            return false;
        string roomCode = roomCodeInput.text.Trim();

        if (string.IsNullOrWhiteSpace(roomCode))
        {
            statusText.text = "Enter a room code first";
            return false;
        }
        bootstrap.DefaultRoomName = roomCode;
        isStarting = true;
        connectionDeadline = Time.unscaledTimeAsDouble + 30d;

        hostButton.interactable = false;
        joinButton.interactable = false;
        statusText.text = (hosting ? "Creating" : "Joining") + " room";

        return true;
    }

    public void CreateRoom()
    {
        if (NetworkProjectConfig.Global.PeerMode != NetworkProjectConfig.PeerModes.Multiple)
        {
            statusText.text = "Local room creation needs Fusion Peer Mode set to Multiple.";
            return;
        }
        if (!TryPrepareConnection(true))
            return;
        // Local development: one non-playing server and one separate client for the creator.
        bootstrap.StartServerPlusClients(1);
    }

    public void JoinGame()
    {
        if (!TryPrepareConnection(false))
            return;
        bootstrap.StartClient();
    }

    public void StartNetworkGame()
    {
        if (lobby == null || !lobby.IsLocalLeader)
        {
            statusText.text = "Only the lobby leader can request Start Game.";
            return;
        }
        if (lobby.RequestStartGame())
            statusText.text = "Requesting game start...";
    }

    private NetworkRunner GetConnectedRunner()
    {
        NetworkRunner runner = NetworkRunner.GetRunnerForGameObject(gameObject);

        bool connected = runner != null && runner.IsRunning && (runner.IsServer || runner.IsConnectedToServer);

        return connected ? runner : null;
    }

    private void SelectInitialClientView(NetworkRunner runner)
    {
        // A locally hosted server is not the creator's playable view.
        foreach (NetworkRunner other in NetworkRunner.Instances)
        {
            if (other != null && other != runner && other.IsRunning && !other.IsServer &&
                other.LocalPlayer != PlayerRef.None && other.LocalPlayer.AsIndex < runner.LocalPlayer.AsIndex)
            {
                runner.SetVisible(false);
                runner.ProvideInput = false;
                return;
            }
        }
        foreach (NetworkRunner other in NetworkRunner.Instances)
        {
            if (other == null || !other.IsRunning)
                continue;
            other.SetVisible(other == runner);
            other.ProvideInput = other == runner;
        }
    }
}
