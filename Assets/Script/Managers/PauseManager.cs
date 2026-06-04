using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour {
    [Header("UI References")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button menuButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button optionsButton;

    [Header("External Systems")]
    [SerializeField] private SettingsMenu settingsMenu;

    [Header("Audio Settings")]
    [Tooltip("The exact SFX name in your AudioManager to play on click")]
    [SerializeField] private string buttonClickSFX = "Testing_ButtonClick";

    private bool isPaused = false;

    private void Awake() {
        var systems = FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsSortMode.None);

        if (systems.Length > 1) {
            var myEventSystem = GetComponentInChildren<UnityEngine.EventSystems.EventSystem>();
            if (myEventSystem != null) {
                Destroy(myEventSystem.gameObject);
            }
        }
    }

    private void Start() {
        if (resumeButton != null) {
            resumeButton.onClick.RemoveAllListeners();
            resumeButton.onClick.AddListener(ResumeGame);
        }

        if (menuButton != null) {
            menuButton.onClick.RemoveAllListeners();
            menuButton.onClick.AddListener(GoToMenu);
        }

        if (restartButton != null) {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(RestartLevel);
        }

        // --- 3. Setup Options Button ---
        if (optionsButton != null) {
            optionsButton.onClick.RemoveAllListeners();
            optionsButton.onClick.AddListener(OnOptionsClicked);
        }

        if (pausePanel != null) pausePanel.SetActive(false);
    }

    private void Update() {
        // --- THE FIX 1: Never allow pausing/unpausing while the scene is loading/fading! ---
        if (GameSceneManager.Instance != null && GameSceneManager.Instance.IsTransitioning) {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P)) {

            // --- THE FIX: We now check 'settingsMenu.IsMenuOpen' ---
            if (isPaused && settingsMenu != null && settingsMenu.IsMenuOpen) {
                settingsMenu.CloseOptions();
                PlayClickSound();
                return;
            }

            // Normal Pause/Unpause
            if (isPaused) {
                ResumeGame();
            } else {
                PauseGame();
            }
        }
    }

    public void PauseGame() {
        isPaused = true;
        Time.timeScale = 0f;

        // --- THE UPGRADE: Freeze all 3D and 2D audio in the world! ---
        AudioListener.pause = true;

        if (pausePanel != null) pausePanel.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        PlayClickSound(); // UI sound will still play (see the setup step below!)
    }

    public void ResumeGame() {
        isPaused = false;
        Time.timeScale = 1f;

        // --- THE UPGRADE: Unfreeze the audio! ---
        AudioListener.pause = false;

        if (pausePanel != null) pausePanel.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        PlayClickSound();
    }

    private void GoToMenu() {
        Time.timeScale = 1f;
        AudioListener.pause = false; // Failsafe: Ensure audio is unpaused for the next scene

        PlayClickSound();

        if (GameSceneManager.Instance != null)
            GameSceneManager.Instance.FadeScene("MainMenu");
    }

    private void RestartLevel() {
        Time.timeScale = 1f;
        AudioListener.pause = false; // Failsafe: Ensure audio is unpaused for the next scene

        PlayClickSound();

        string currentSceneName = SceneManager.GetActiveScene().name;
        if (GameSceneManager.Instance != null)
            GameSceneManager.Instance.FadeScene(currentSceneName);
    }

    private void OnOptionsClicked() {
        PlayClickSound();

        if (settingsMenu != null) {
            settingsMenu.OpenOptions();
        } else {
            Debug.LogWarning("SettingsMenu reference is missing in MainMenuUI!");
        }
    }

    private void PlayClickSound() {
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(buttonClickSFX)) {
            AudioManager.Instance.PlayUI(buttonClickSFX);
        }
    }
}