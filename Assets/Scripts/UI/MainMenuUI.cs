using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private GameObject controlsPanel, darkOverlay;
    [SerializeField] private GameObject playPanel, enterRoomCodePanel;
    [SerializeField] private TMP_InputField roomCodeInput;
    [SerializeField] private Button roomSubmitButton;
    private TMP_Text roomStatus;
    private bool creating, connecting;

    private void Awake()
    {
        SerializedFieldValidator.Validate(this);
        controlsPanel.SetActive(false);
        playPanel.SetActive(false);
        enterRoomCodePanel.SetActive(false);
        darkOverlay.SetActive(false);
        // Reuse the existing button's font so the connection message matches this menu.
        GameObject label = new GameObject("RoomStatus", typeof(RectTransform));
        label.transform.SetParent(enterRoomCodePanel.transform, false);
        label.layer = enterRoomCodePanel.layer;
        roomStatus = label.AddComponent<TextMeshProUGUI>();
        roomStatus.font = roomSubmitButton.GetComponentInChildren<TMP_Text>(true).font;
        roomStatus.text = "";
        roomStatus.fontSize = 25f;
        roomStatus.enableAutoSizing = true;
        roomStatus.fontSizeMin = 18f;
        roomStatus.fontSizeMax = 25f;
        roomStatus.color = new Color(0.5f, 0.08f, 0.06f);
        roomStatus.alignment = TextAlignmentOptions.Center;
        roomStatus.raycastTarget = false;
        roomStatus.richText = false;
        roomStatus.rectTransform.sizeDelta = new Vector2(830f, 65f);
        roomStatus.rectTransform.anchoredPosition = new Vector2(0f, -76f);
        if (NetworkSessionBootstrap.ReturnToMainMenu)
        {
            ShowRoomPanel(NetworkSessionBootstrap.MenuCreating);
            roomCodeInput.SetTextWithoutNotify(NetworkSessionBootstrap.MenuRoomCode);
            roomStatus.text = NetworkSessionBootstrap.MenuMessage;
        }
    }

    public void PlayGame()
    {
        if (connecting)
            return;
        controlsPanel.SetActive(false);
        enterRoomCodePanel.SetActive(false);
        darkOverlay.SetActive(true);
        playPanel.SetActive(true);
    }

    public void CreateRoom() => ShowRoomPanel(true);
    public void JoinRoom() => ShowRoomPanel(false);

    private void ShowRoomPanel(bool create)
    {
        if (connecting)
            return;
        creating = create;
        controlsPanel.SetActive(false);
        playPanel.SetActive(false);
        darkOverlay.SetActive(true);
        enterRoomCodePanel.SetActive(true);
        roomSubmitButton.GetComponentInChildren<TMP_Text>(true).text = create ? "Create Room" : "Join Room";
        roomStatus.text = "";
    }

    public void SubmitRoom()
    {
        if (connecting)
            return;
        string code = roomCodeInput.text.Trim();
        if (string.IsNullOrWhiteSpace(code))
        {
            roomStatus.text = "Enter a room code first.";
            return;
        }
        if (creating && Fusion.NetworkProjectConfig.Global.PeerMode != Fusion.NetworkProjectConfig.PeerModes.Multiple)
        {
            roomStatus.text = "Room creation requires Fusion Peer Mode set to Multiple.";
            return;
        }
        if (!Application.CanStreamedLevelBeLoaded("MultiplayerSandbox"))
        {
            roomStatus.text = "MultiplayerSandbox is missing from the build scene list.";
            return;
        }
        connecting = true;
        roomSubmitButton.interactable = false;
        roomCodeInput.interactable = false;
        roomStatus.text = creating ? "Creating room..." : "Joining room...";
        NetworkSessionBootstrap.QueueMenuConnection(code, creating);
        SceneManager.LoadSceneAsync("MultiplayerSandbox");
    }

    public void ShowControls()
    {
        if (connecting)
            return;
        playPanel.SetActive(false);
        enterRoomCodePanel.SetActive(false);
        controlsPanel.SetActive(true);
        darkOverlay.SetActive(true);
    }

    public void HideControls()
    {
        if (connecting)
            return;
        playPanel.SetActive(false);
        enterRoomCodePanel.SetActive(false);
        controlsPanel.SetActive(false);
        darkOverlay.SetActive(false);
    }

    public void ExitGame()
    {
        Application.Quit();
    }
}
