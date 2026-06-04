using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameManager : MonoBehaviour {
    public static GameManager Instance { get; private set; }

    [Header("Scene Transitions")]
    [Tooltip("Type the exact name of your Game Over scene here")]
    [SerializeField] private string gameOverSceneName = "GameOver";

    [Tooltip("Type the exact name of your Victory scene here")]
    [SerializeField] private string victorySceneName = "Victory";

    // Notice we completely removed the PlayerController variable here!

    private bool isGameEnding = false;

    void Awake() {
        if (Instance == null) {
            Instance = this;
        } else {
            Destroy(gameObject);
        }
    }

    // --- 1. THE DEFEAT SEQUENCE ---
    public void TriggerGameOver() {
        if (isGameEnding) return;
        isGameEnding = true;

        Debug.Log("<color=red>GAME OVER! Player has died.</color>");

        // --- THE UPGRADE: Find the player dynamically exactly when we need to lock them ---
        PlayerController activePlayer = FindFirstObjectByType<PlayerController>();
        if (activePlayer != null) {
            activePlayer.SetPlayerControl(false);
        }

        // Slow down time for a dramatic death!
        Time.timeScale = 0.3f;

        // Wait 4 seconds (in real time), then load the Game Over scene
        StartCoroutine(TransitionToSceneRoutine(gameOverSceneName, 4f));
    }

    // --- 2. THE VICTORY SEQUENCE ---
    public void TriggerVictory() {
        if (isGameEnding) return;
        isGameEnding = true;

        Debug.Log("<color=yellow>VICTORY! Boss has died.</color>");

        // --- THE UPGRADE: Find the player dynamically! ---
        PlayerController activePlayer = FindFirstObjectByType<PlayerController>();
        if (activePlayer != null) {
            activePlayer.SetPlayerControl(false);
        }

        // Wait a few seconds to watch the boss die, then load the Victory scene
        StartCoroutine(TransitionToSceneRoutine(victorySceneName, 5f));
    }

    // --- 3. THE SAFE SCENE LOADER ---
    IEnumerator TransitionToSceneRoutine(string targetSceneName, float delayInSeconds) {
        // We use Realtime because Time.timeScale might be modified!
        yield return new WaitForSecondsRealtime(delayInSeconds);

        // ALWAYS reset time back to normal before leaving the scene!
        Time.timeScale = 1f;

        // Try to use your custom fade manager
        if (GameSceneManager.Instance != null) {
            GameSceneManager.Instance.FadeScene(targetSceneName);
        } else {
            Debug.LogWarning("SceneManage Instance not found. Using default Unity scene loader.");
            SceneManager.LoadScene(targetSceneName);
        }
    }
}