using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class NetworkLauncher : MonoBehaviour
{
    [SerializeField] private FusionBootstrap bootstrap;
    [SerializeField] private TMP_InputField roomCodeInput;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Button hostButton, joinButton, startGameButton;
    [SerializeField] private string multiplayerGameScenePath = "Assets/Scenes/MultiplayerGame.unity";
    private bool isStarting, isHosting;

    private void Awake()
    {
        SerializedFieldValidator.Validate(this);
    }

    private void Update()
    {
        NetworkRunner runner = GetConnectedRunner();
        startGameButton.gameObject.SetActive(runner != null && runner.IsSceneAuthority);

        if (!isStarting || runner == null)
            return;

        statusText.text = isHosting ? "Room ready, waiting for players" : "Joined room";
        isStarting = false;
    }

    private bool TryPrepareConnection(bool hosting)
    {
        string roomCode = roomCodeInput.text.Trim();

        if (string.IsNullOrWhiteSpace(roomCode))
        {
            statusText.text = "Enter a room code first";
            return false;
        }
        bootstrap.DefaultRoomName = roomCode;
        isStarting = true;
        isHosting = hosting;

        hostButton.interactable = false;
        joinButton.interactable = false;
        statusText.text = (hosting ? "Creating" : "Joining") + " room";

        return true;
    }

    public void HostGame()
    {
        if (!TryPrepareConnection(true))
            return;
        bootstrap.StartHost();
    }

    public void JoinGame()
    {
        if (!TryPrepareConnection(false))
            return;
        bootstrap.StartClient();
    }

    public void StartNetworkGame()
    {
        NetworkRunner runner = GetConnectedRunner();
        if (runner == null || !runner.IsSceneAuthority)
        {
            statusText.text = "Only the host can start the game";
            return;
        }

        SceneRef gameScene = runner.GetSceneRef(multiplayerGameScenePath);

        if (!gameScene.IsValid)
        {
            statusText.text = "MultiplayerGame is missing from the build";
            return;
        }
        startGameButton.interactable = false;
        statusText.text = "Loading game";

        runner.LoadScene(gameScene, LoadSceneMode.Single);
    }

    private NetworkRunner GetConnectedRunner()
    {
        foreach (NetworkRunner runner in NetworkRunner.Instances)
        {
            bool connected = runner != null && runner.IsRunning && (runner.IsServer || runner.IsConnectedToServer);
            if (connected)
                return runner;
        }
        return null;
    }
}