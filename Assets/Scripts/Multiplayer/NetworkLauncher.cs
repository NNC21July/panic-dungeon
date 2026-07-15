using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NetworkLauncher : MonoBehaviour
{
    [SerializeField] private FusionBootstrap bootstrap;
    [SerializeField] private TMP_InputField roomCodeInput;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Button hostButton, joinButton;
    private bool isStarting, isHosting;

    private void Awake()
    {
        SerializedFieldValidator.Validate(this);
    }

    private void Update()
    {
        if (!isStarting)
            return;

        foreach (NetworkRunner runner in NetworkRunner.Instances)
        {
            bool connected = runner != null && runner.IsRunning && (runner.IsServer || runner.IsConnectedToServer);
            if (!connected)
                continue;

            statusText.text = isHosting ? "Room ready, waiting for players" : "Joined room";

            isStarting = false;
            break;
        }
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
}