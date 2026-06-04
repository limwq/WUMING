using UnityEngine;
using UnityEngine.Playables;
using System.Collections.Generic;

public class CutsceneManager : MonoBehaviour {
    [Header("Timeline Settings")]
    public PlayableDirector timelineDirector;

    [Tooltip("If true, plays the moment the scene loads. If false, waits for another script to call StartCutscene()")]
    public bool playOnAwake = false;

    [Header("Master References")]
    public PlayerController playerController;
    public GameObject mainHUD;

    [Tooltip("Drag the GameObject holding your CutsceneSkipManager and UI button here")]
    public GameObject skipManagerUI;

    [Tooltip("Drag the Real Boss here so the cutscene can yell 'Action!' when it finishes")]
    public BossBrain bossBrain;

    [Header("Save Settings")]
    [Tooltip("Check this if you want the game to remember this cutscene has been played so it skips next time.")]
    public bool saveCutscene = true;

    [Tooltip("Give this a unique name (e.g., 'Boss_Intro_01'). Required if Save Cutscene is checked!")]
    public string cutsceneID = "";

    [Header("Post-Cutscene World State")]
    public List<GameObject> objectsToEnable;
    public List<GameObject> objectsToDisable;

    [Header("Scene Transition")]
    [Tooltip("Check this if the game should load a new scene when the cutscene ends.")]
    public bool loadSceneAfterCutscene = false;
    [Tooltip("The EXACT name of the scene to load (e.g., 'Credits' or 'Level_2').")]
    public string sceneToLoad = "";

    private bool hasPlayed = false;

    void Start() {
        if (saveCutscene && !string.IsNullOrEmpty(cutsceneID) && SaveManager.Instance != null) {
            SaveManager.GameData data = SaveManager.Instance.LoadGame();
            if (data != null && data.playedCutscenes.Contains(cutsceneID)) {

                // Hide the skip button immediately since we are bypassing the cutscene
                if (skipManagerUI != null) skipManagerUI.SetActive(false);

                ApplyPostCutsceneState();

                if (loadSceneAfterCutscene && !string.IsNullOrEmpty(sceneToLoad)) {
                    LoadNextScene();
                } else {
                    Destroy(gameObject);
                }
                return;
            }
        }

        if (playOnAwake) {
            StartCutscene();
        }
    }

    public void StartCutscene() {
        if (hasPlayed) return;

        hasPlayed = true;

        if (playerController != null) playerController.SetPlayerControl(false);
        if (mainHUD != null) mainHUD.SetActive(false);

        // --- THE UPDATE: Show Skip Button AND Unlock the Cursor! ---
        if (skipManagerUI != null) {
            skipManagerUI.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (timelineDirector != null) {
            timelineDirector.Play();
            timelineDirector.stopped += OnCutsceneFinished;
        }
    }

    void OnCutsceneFinished(PlayableDirector director) {
        // FAILSAFE 1: Is the Pause Menu already transitioning us away?
        if (GameSceneManager.Instance != null && GameSceneManager.Instance.IsTransitioning) {
            Debug.Log("<color=yellow>[CutsceneManager] Aborting finish logic. A scene transition is already happening!</color>");
            return;
        }

        if (playerController != null) playerController.SetPlayerControl(true);
        if (mainHUD != null) mainHUD.SetActive(true);

        // --- THE UPDATE: Hide Skip Button AND Relock the Cursor for gameplay! ---
        if (skipManagerUI != null) {
            skipManagerUI.SetActive(false);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        ApplyPostCutsceneState();

        if (saveCutscene && !string.IsNullOrEmpty(cutsceneID) && SaveManager.Instance != null) {
            SaveManager.Instance.SaveWorldEvent("Cutscene", cutsceneID);
        }

        director.stopped -= OnCutsceneFinished;

        if (loadSceneAfterCutscene && !string.IsNullOrEmpty(sceneToLoad)) {
            LoadNextScene();
        }
    }

    private void ApplyPostCutsceneState() {
        if (objectsToEnable != null) {
            foreach (GameObject obj in objectsToEnable) {
                if (obj != null) obj.SetActive(true);
            }
        }

        if (objectsToDisable != null) {
            foreach (GameObject obj in objectsToDisable) {
                if (obj != null) obj.SetActive(false);
            }
        }

        if (bossBrain != null) {
            bossBrain.StartBossFight();
            Debug.Log("<color=cyan>[CutsceneManager] Yelled ACTION! Boss is waking up.</color>");
        }
    }

    private void LoadNextScene() {
        Debug.Log($"<color=cyan>[CutsceneManager] Transitioning to scene: {sceneToLoad}</color>");

        if (GameSceneManager.Instance != null) {
            GameSceneManager.Instance.FadeScene(sceneToLoad);
        } else {
            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneToLoad);
        }
    }
}