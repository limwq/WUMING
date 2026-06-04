using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour {
    [Header("Buttons")]
    [SerializeField] private Button continueButton;
    [SerializeField] private Button playButton;
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button quitButton;

    [Header("External Systems")]
    [SerializeField] private SettingsMenu settingsMenu;

    [Header("Scene Transitions")]
    [Tooltip("Type the exact name of the scene to load when hitting New Game/Play")]
    [SerializeField] private string startingLevelName = "Level_1";

    [Header("Audio Settings")]
    [Tooltip("The exact SFX name in your AudioManager to play on click")]
    [SerializeField] private string buttonClickSFX = "Testing_ButtonClick";

    private void Start() {
        if (AudioManager.Instance != null) {
            AudioManager.Instance.CrossfadeMusic("BGM_MainMenu", 1f);
        }

        // Ensure the mouse is ALWAYS visible on the Main Menu!
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // --- 1. Setup Continue Button ---
        if (continueButton != null) {
            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(OnContinueClicked);

            // Safely check if a save file exists to enable the button
            if (SaveManager.Instance != null) {
                SaveManager.GameData data = SaveManager.Instance.LoadGame();
                // Enable the button ONLY if data exists and has a valid scene name
                continueButton.interactable = (data != null && !string.IsNullOrEmpty(data.sceneName));
            } else {
                continueButton.interactable = false;
            }
        }

        // --- 2. Setup Play (New Game) Button ---
        if (playButton != null) {
            playButton.onClick.RemoveAllListeners();
            playButton.onClick.AddListener(OnPlayClicked);
        }

        // --- 3. Setup Options Button ---
        if (optionsButton != null) {
            optionsButton.onClick.RemoveAllListeners();
            optionsButton.onClick.AddListener(OnOptionsClicked);
        }

        // --- 4. Setup Quit Button ---
        if (quitButton != null) {
            quitButton.onClick.RemoveAllListeners();
            quitButton.onClick.AddListener(OnQuitClicked);
        }
    }

    private void PlayClickSound() {
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(buttonClickSFX)) {
            AudioManager.Instance.PlayUI(buttonClickSFX);
        }
    }

    private void OnContinueClicked() {
        PlayClickSound();

        if (SaveManager.Instance != null && GameSceneManager.Instance != null) {
            SaveManager.GameData data = SaveManager.Instance.LoadGame();

            if (data != null && !string.IsNullOrEmpty(data.sceneName)) {
                Debug.Log($"<color=green>Loading Save File: {data.sceneName}</color>");
                GameSceneManager.Instance.FadeScene(data.sceneName);
            }
        }
    }

    // --- UPDATED: The New Game Logic ---
    private void OnPlayClicked() {
        PlayClickSound();

        // --- THE UPGRADE: Wipe the old save file! ---
        if (SaveManager.Instance != null) {
            SaveManager.Instance.ClearSaveData();
            Debug.Log("<color=yellow>[MainMenuUI] Old save data wiped. Starting fresh!</color>");
        } else {
            Debug.LogWarning("SaveManager is missing! Cannot clear old save data.");
        }

        if (GameSceneManager.Instance != null && !string.IsNullOrEmpty(startingLevelName)) {
            GameSceneManager.Instance.FadeScene(startingLevelName);
        }
    }

    private void OnOptionsClicked() {
        PlayClickSound();

        if (settingsMenu != null) {
            settingsMenu.OpenOptions();
        } else {
            Debug.LogWarning("SettingsMenu reference is missing in MainMenuUI!");
        }
    }

    private void OnQuitClicked() {
        PlayClickSound();
        Debug.Log("Quitting Game...");
        Application.Quit();
    }
}