using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private string gameplaySceneName = "SampleScene";
    [SerializeField] private Button playButton;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;

        TryCreateForScene(SceneManager.GetActiveScene());
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryCreateForScene(scene);
    }

    static void TryCreateForScene(Scene scene)
    {
        if (scene.name != "MainMenu")
            return;

        if (FindAnyObjectByType<MainMenuController>() != null)
            return;

        GameObject controller = new GameObject("Main Menu Controller");
        controller.AddComponent<MainMenuController>();
    }

    void Awake()
    {
        if (playButton == null)
            playButton = FindPlayButton();

        if (playButton != null)
            playButton.onClick.AddListener(Play);
        else
            Debug.LogWarning("Play button was not found in MainMenu scene.");
    }

    void OnDestroy()
    {
        if (playButton != null)
            playButton.onClick.RemoveListener(Play);
    }

    public void Play()
    {
        SceneManager.LoadScene(gameplaySceneName);
    }

    Button FindPlayButton()
    {
        Button[] buttons = FindObjectsByType<Button>(
            FindObjectsInactive.Include);

        foreach (Button button in buttons)
        {
            if (button.name == "play")
                return button;
        }

        return null;
    }
}
