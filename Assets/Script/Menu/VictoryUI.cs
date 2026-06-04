using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video; // --- NEW: Required to talk to the VideoPlayer ---

public class VictoryUI : MonoBehaviour {
    [Header("Video Settings")]
    [Tooltip("Drag the GameObject with the VideoPlayer component here")]
    [SerializeField] private VideoPlayer victoryVideo;

    [Header("Buttons")]
    [Tooltip("This button now acts as a 'Skip' button to leave early")]
    [SerializeField] private Button menuButton;

    [Header("Audio Settings")]
    [Tooltip("The exact SFX name in your AudioManager to play on click")]
    [SerializeField] private string buttonClickSFX = "Testing_ButtonClick";

    private void Start() {
        // Unlock the mouse so they can click the skip button if they want to
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // --- NEW: Subscribe to the VideoPlayer event ---
        if (victoryVideo != null) {
            // 'loopPointReached' fires automatically when the video finishes playing
            victoryVideo.loopPointReached += OnVideoFinished;
        } else {
            Debug.LogWarning("[VictoryUI] No VideoPlayer assigned! The game won't auto-transition.");
        }

        if (menuButton != null) {
            menuButton.onClick.RemoveAllListeners();
            menuButton.onClick.AddListener(OnMenuClicked);
        }
    }

    private void OnDestroy() {
        // --- AAA FIX: Always unsubscribe from events when the object is destroyed to prevent memory leaks! ---
        if (victoryVideo != null) {
            victoryVideo.loopPointReached -= OnVideoFinished;
        }
    }

    private void PlayClickSound() {
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(buttonClickSFX)) {
            AudioManager.Instance.PlayUI(buttonClickSFX);
        }
    }

    // --- EVENT: Triggered automatically by the VideoPlayer ---
    private void OnVideoFinished(VideoPlayer vp) {
        Debug.Log("<color=cyan>[VictoryUI] Cinematic finished normally. Transitioning to Main Menu.</color>");
        ExecuteEndSequence();
    }

    // --- EVENT: Triggered by the Player clicking the button ---
    private void OnMenuClicked() {
        Debug.Log("<color=orange>[VictoryUI] Player skipped the cinematic.</color>");
        PlayClickSound();

        // Optional: Stop the video immediately if they click skip so audio doesn't bleed during the fade
        if (victoryVideo != null) victoryVideo.Stop();

        ExecuteEndSequence();
    }

    // --- SHARED LOGIC: We put this in one method so both events can use it ---
    private void ExecuteEndSequence() {
        if (GameSceneManager.Instance != null) {
            // Fading back to the Main Menu will automatically wipe the Boss Music lock!
            GameSceneManager.Instance.FadeScene("MainMenu");
        }

        // Wipe the old save file!
        if (SaveManager.Instance != null) {
            SaveManager.Instance.ClearSaveData();
            Debug.Log("<color=yellow>[VictoryUI] Old save data wiped. Starting fresh!</color>");
        } else {
            Debug.LogWarning("SaveManager is missing! Cannot clear old save data.");
        }
    }
}