using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic; // --- NEW: Required for Lists ---

public class GameSceneManager : MonoBehaviour {
    // Standard Singleton Instance
    public static GameSceneManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private CanvasGroup loadingCanvasGroup;
    [SerializeField] private Slider loadingBar;

    [Header("Settings")]
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private float minLoadTime = 1.0f;

    // --- NEW: Global flag to prevent other scripts from interrupting a scene load! ---
    public bool IsTransitioning { get; private set; }

    // --- NEW: BGM Mapping ---
    [System.Serializable]
    public struct SceneBGM {
        [Tooltip("The exact name of the Scene in your Build Settings")]
        public string sceneName;
        [Tooltip("The exact audio tag from your AudioManager")]
        public string bgmTag;
    }

    [Header("Audio Settings")]
    [Tooltip("Map scenes to music here. If a scene is NOT in this list (like GameOver), the music will just keep playing!")]
    public List<SceneBGM> sceneMusicMap;

    void Awake() {
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Initialize();
        } else {
            Destroy(gameObject);
        }
    }

    private void Initialize() {
        loadingCanvasGroup.alpha = 0;
        loadingCanvasGroup.blocksRaycasts = false;
        if (loadingBar != null) loadingBar.gameObject.SetActive(false);
    }

    public void LoadScene(string sceneName) {
        StartCoroutine(LoadSceneRoutine(sceneName));
    }

    private IEnumerator LoadSceneRoutine(string sceneName) {
        IsTransitioning = true;

        // =========================================================
        // --- THE AAA CLEANUP: NUKE EVERYTHING BEFORE FADING! ---
        // =========================================================

        // 1. Silence Audio
        if (AudioManager.Instance != null) AudioManager.Instance.StopAllSFX();

        // 2. Erase all surviving Particles and Bullets
        if (VFXManager.Instance != null) VFXManager.Instance.StopAllVisuals();

        // 3. Find the player and lock them down (USING NEW UNITY SYNTAX)
        PlayerController player = Object.FindFirstObjectByType<PlayerController>();
        if (player != null) player.SetPlayerControl(false);

        // 4. Find EVERY enemy and lobotomize them (USING NEW UNITY SYNTAX)
        EnemyBase[] allEnemies = Object.FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
        foreach (EnemyBase enemy in allEnemies) {
            enemy.StopAllCoroutines();
            enemy.enabled = false;
        }

        // --- PHASE 1: FADE IN ---
        loadingCanvasGroup.blocksRaycasts = true;
        if (loadingBar != null) loadingBar.gameObject.SetActive(false);

        float fadeTimer = 0f;
        while (fadeTimer < fadeDuration) {
            fadeTimer += Time.deltaTime;
            loadingCanvasGroup.alpha = fadeTimer / fadeDuration;
            yield return null;
        }
        loadingCanvasGroup.alpha = 1;

        // --- PHASE 2: SHOW BAR & LOAD ---
        if (loadingBar != null) {
            loadingBar.gameObject.SetActive(true);
            loadingBar.value = 0;
        }

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        operation.allowSceneActivation = false;

        float loadTimer = 0f;

        while (loadTimer < minLoadTime || operation.progress < 0.9f) {
            loadTimer += Time.deltaTime;
            float simulatedProgress = Mathf.Clamp01(loadTimer / minLoadTime);
            if (loadingBar != null) loadingBar.value = simulatedProgress;
            yield return null;
        }

        // --- PHASE 3: HIDE BAR, SWAP SCENE, & UPDATE AUDIO ---
        if (loadingBar != null) loadingBar.gameObject.SetActive(false);

        operation.allowSceneActivation = true;

        while (!operation.isDone) {
            yield return null;
        }

        // --- NEW: Trigger the Scene Music! ---
        PlaySceneMusic(sceneName);

        // --- PHASE 4: FADE OUT ---
        fadeTimer = 0f;
        while (fadeTimer < fadeDuration) {
            fadeTimer += Time.deltaTime;
            loadingCanvasGroup.alpha = 1f - (fadeTimer / fadeDuration);
            yield return null;
        }

        loadingCanvasGroup.alpha = 0;
        loadingCanvasGroup.blocksRaycasts = false;
    }

    public void FadeScene(string sceneName) {
        StartCoroutine(FadeSceneRoutine(sceneName));
    }

    private IEnumerator FadeSceneRoutine(string sceneName) {
        IsTransitioning = true;

        // =========================================================
        // --- THE AAA CLEANUP: NUKE EVERYTHING BEFORE FADING! ---
        // =========================================================

        // 1. Silence Audio
        if (AudioManager.Instance != null) AudioManager.Instance.StopAllSFX();

        // 2. Erase all surviving Particles and Bullets
        if (VFXManager.Instance != null) VFXManager.Instance.StopAllVisuals();

        // 3. Find the player and lock them down (USING NEW UNITY SYNTAX)
        PlayerController player = Object.FindFirstObjectByType<PlayerController>();
        if (player != null) player.SetPlayerControl(false);

        // 4. Find EVERY enemy and lobotomize them (USING NEW UNITY SYNTAX)
        EnemyBase[] allEnemies = Object.FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
        foreach (EnemyBase enemy in allEnemies) {
            enemy.StopAllCoroutines();
            enemy.enabled = false;
        }

        // --- PHASE 1: FADE IN ---
        loadingCanvasGroup.blocksRaycasts = true;

        if (loadingBar != null) loadingBar.gameObject.SetActive(false);

        float fadeTimer = 0f;
        while (fadeTimer < fadeDuration) {
            fadeTimer += Time.deltaTime;
            loadingCanvasGroup.alpha = fadeTimer / fadeDuration;
            yield return null;
        }
        loadingCanvasGroup.alpha = 1;

        // --- PHASE 2: LOAD ---
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        operation.allowSceneActivation = false;

        float loadTimer = 0f;

        while (loadTimer < minLoadTime || operation.progress < 0.9f) {
            loadTimer += Time.deltaTime;
            yield return null;
        }

        // --- PHASE 3: SWAP SCENE & UPDATE AUDIO ---
        operation.allowSceneActivation = true;

        while (!operation.isDone) {
            yield return null;
        }

        // --- NEW: Trigger the Scene Music! ---
        PlaySceneMusic(sceneName);

        // --- PHASE 4: FADE OUT ---
        fadeTimer = 0f;
        while (fadeTimer < fadeDuration) {
            fadeTimer += Time.deltaTime;
            loadingCanvasGroup.alpha = 1f - (fadeTimer / fadeDuration);
            yield return null;
        }

        loadingCanvasGroup.alpha = 0;
        loadingCanvasGroup.blocksRaycasts = false;

        if (player != null) player.SetPlayerControl(true);

        IsTransitioning = false;
    }

    // --- NEW: Audio Helper Method ---
    private void PlaySceneMusic(string newSceneName) {
        if (AudioManager.Instance == null) return;

        foreach (SceneBGM mapping in sceneMusicMap) {
            if (mapping.sceneName == newSceneName) {
                // If the scene maps to a specific track, crossfade to it!
                if (!string.IsNullOrEmpty(mapping.bgmTag)) {
                    if (mapping.bgmTag == "BGM_MainMenu") {
                        AudioManager.Instance.ClearBossMusic();
                        AudioManager.Instance.CrossfadeMusic("BGM_MainMenu", 1.5f);
                    } else if (mapping.bgmTag == "BGM_Normal") {
                        AudioManager.Instance.ClearBossMusic();
                        AudioManager.Instance.CrossfadeMusic("BGM_Normal", 1.5f);
                    } else {
                        AudioManager.Instance.CrossfadeMusic(mapping.bgmTag, 1.5f);
                    }
                }
                // --- THE FIX: If the tag is explicitly blank, STOP the music! ---
                else {
                    AudioManager.Instance.ClearBossMusic();
                    AudioManager.Instance.StopMusic();
                    Debug.Log($"<color=yellow>[GameSceneManager] BGM tag is blank for '{newSceneName}'. Stopping music.</color>");
                }
                return; // We found the scene, stop searching!
            }
        }

        // If the loop finishes and we didn't find the scene at all
        Debug.Log($"<color=cyan>[GameSceneManager] No BGM mapped for '{newSceneName}'. Keeping current track.</color>");
    }
}