using UnityEngine;
using UnityEngine.UI;

public class SkipManager : MonoBehaviour {
    [Header("Scene Transition")]
    [Tooltip("The exact name of the scene to load after skipping (e.g., 'Level_01')")]
    [SerializeField] private string targetSceneName;

    [Header("UI References")]
    [Tooltip("Drag your UI Skip Button here")]
    [SerializeField] private Button skipButton;

    [Header("Audio Settings")]
    [SerializeField] private string clickSfxTag = "Testing_ButtonClick";

    private bool hasSkipped = false;

    private void Start() {
        // Automatically hook up the button click event just like the PauseMenu!
        if (skipButton != null) {
            skipButton.onClick.RemoveAllListeners();
            skipButton.onClick.AddListener(SkipCutscene);
        } else {
            Debug.LogWarning("<color=yellow>[CutsceneSkipManager] No button assigned in the Inspector!</color>");
        }
    }

    public void SkipCutscene() {
        // Prevent double-clicking if they mash the button
        if (hasSkipped || (GameSceneManager.Instance != null && GameSceneManager.Instance.IsTransitioning)) return;

        hasSkipped = true;

        // Play the UI click sound
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(clickSfxTag)) {
            AudioManager.Instance.PlayUI(clickSfxTag);
        }

        Debug.Log("<color=magenta>[CutsceneSkip] Skipping to: " + targetSceneName + "</color>");

        // Let the GameSceneManager handle the fade, audio stopping, and cleanup!
        if (GameSceneManager.Instance != null) {
            GameSceneManager.Instance.FadeScene(targetSceneName);
        } else {
            Debug.LogError("[CutsceneSkipManager] GameSceneManager is missing!");
        }
    }

    // --- Optional: Call this via Animation Event when the cutscene naturally ends! ---
    public void OnCutsceneFinished() {
        if (!hasSkipped) {
            SkipCutscene();
        }
    }
}