using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuUI : MonoBehaviour
{
    private const string GameSceneName = "Game";
    [SerializeField] private GameObject controlsPanel;

    private void Awake()
    {
        SerializedFieldValidator.Validate(this);
        controlsPanel.SetActive(false);
    }

    public void PlayGame()
    {
        SceneManager.LoadScene(GameSceneName);
    }

    public void ShowControls()
    {
        controlsPanel.SetActive(true);
    }

    public void HideControls()
    {
        controlsPanel.SetActive(false);
    }

    public void ExitGame()
    {
        Application.Quit();
    }
}