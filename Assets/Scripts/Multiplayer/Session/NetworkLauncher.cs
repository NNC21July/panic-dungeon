using Fusion;
using TMPro;
using NaughtyAttributes;
using UnityEngine;

// Presents the waiting lobby; room creation and joining belong to MainMenuUI.
public class NetworkLauncher : MonoBehaviour
{
    [SerializeField, Required] private TMP_Text statusText;
    [SerializeField, Required] private GameObject loadingPanel;
    [SerializeField, Required] private NetworkLobbyManager lobby;
    private bool assignedLocalView;
    private NetworkSessionUI sessionUI;

    private void Awake()
    {
        SerializedFieldValidator.Validate(this);
        loadingPanel.SetActive(true);
        statusText.text = "Connecting to room...";
    }

    private void Update()
    {
        NetworkRunner runner = GetConnectedRunner();
        if (runner == null)
        {
            bool idle = NetworkSessionBootstrap.Instance != null &&
                NetworkSessionBootstrap.Instance.CurrentStage == FusionBootstrap.Stage.Disconnected;
            statusText.text = idle
                ? "Open MainMenu to create or join a room.\nEditor testing: start peers from NetworkSessionBootstrap."
                : "Connecting to room...";
            return;
        }
        if (!assignedLocalView && !runner.IsServer)
        {
            SelectInitialClientView(runner);
            assignedLocalView = true;
        }
        bool lobbyReady = lobby != null && lobby.Object != null && lobby.Object.IsValid;
        if (!lobbyReady)
        {
            statusText.text = "Loading lobby...";
            return;
        }
        if (runner.IsServer)
        {
            statusText.text = $"Server running - {lobby.ConnectedPlayers} players connected.";
            return;
        }
        if (sessionUI == null)
            sessionUI = NetworkSessionUI.Create(runner, statusText, lobby);
        loadingPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (sessionUI != null)
            Destroy(sessionUI.gameObject);
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
