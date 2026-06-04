using UnityEngine;
using UnityEngine.UI;

public class GameOverUI : MonoBehaviour {
    [Header("Buttons")]
    [SerializeField] private Button restartButton;
    [SerializeField] private Button menuButton;

    [Header("Audio Settings")]
    [Tooltip("The exact SFX name in your AudioManager to play on click")]
    [SerializeField] private string buttonClickSFX = "Testing_ButtonClick";

    private void Start() {
        // --- 1. ALWAYS UNLOCK THE MOUSE! ---
        // If the player died while locked on, the mouse will be invisible. We must force it back.
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // --- 2. SETUP BUTTONS ---
        if (restartButton != null) {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(OnRestartClicked);
        }

        if (menuButton != null) {
            menuButton.onClick.RemoveAllListeners();
            menuButton.onClick.AddListener(OnMenuClicked);
        }

    }

    private void PlayClickSound() {
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(buttonClickSFX)) {
            AudioManager.Instance.PlayUI(buttonClickSFX);
        }
    }

    private void OnRestartClicked() {
        PlayClickSound();

        if (GameSceneManager.Instance != null) {
            // --- THE AAA RESPAWN ---
            // Load the save file to find out which bonfire scene they last rested at
            if (SaveManager.Instance != null) {
                SaveManager.GameData data = SaveManager.Instance.LoadGame();
                if (data != null && !string.IsNullOrEmpty(data.sceneName)) {
                    GameSceneManager.Instance.FadeScene(data.sceneName);
                    return;
                }
            }

            // Failsafe: If no save exists, just reload Level_1
            Debug.LogWarning("[GameOverUI] No save data found! Falling back to Level_1.");
            GameSceneManager.Instance.FadeScene("Level_1");
        }
    }

    private void OnMenuClicked() {
        PlayClickSound();
        if (GameSceneManager.Instance != null) {
            GameSceneManager.Instance.FadeScene("MainMenu");
        }
    }
}