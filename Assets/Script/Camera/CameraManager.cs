using UnityEngine;
using Unity.Cinemachine; // --- UPDATED FOR CM3 ---
using System.Collections;

public class CameraManager : MonoBehaviour {

    public static CameraManager Instance { get; private set; }

    [Header("Cameras")]
    public CinemachineCamera defaultCam; // --- UPDATED FOR CM3 ---
    public CinemachineCamera aimCam;

    [Header("UI Elements")]
    public GameObject crosshairUI;

    private CinemachineCamera currentCam;

    void Awake() {
        if (Instance == null) {
            Instance = this;
        } else {
            Destroy(gameObject);
        }
    }

    void Start() {
        StartCoroutine(PrewarmCameras());
        SwitchToDefault();
    }

    private IEnumerator PrewarmCameras() {
        // 1. Force the Aim Camera to become the live camera instantly
        if (aimCam != null) {
            aimCam.Priority = 100; // Force it higher than everything
        }

        // 2. Wait exactly ONE frame. 
        // This forces Unity to run all the heavy math and compile the shaders invisibly!
        yield return null;

        // 3. Drop it back down to its resting state
        if (aimCam != null) {
            aimCam.Priority = 0;
        }

        // Note: Because this happens in Start(), it occurs while your GameSceneManager 
        // is still doing its fade-in, so the player never sees the screen jump!
    }

    public void SwitchToAim() {
        ChangeCamera(aimCam);
        if (crosshairUI != null) crosshairUI.SetActive(true);
    }

    public void SwitchToDefault() {
        ChangeCamera(defaultCam);
        if (crosshairUI != null) crosshairUI.SetActive(false);
    }

    public void ChangeCamera(CinemachineCamera newCam) {
        if (newCam == null) return;

        if (currentCam != null) {
            currentCam.Priority = 10;
        }

        currentCam = newCam;
        currentCam.Priority = 20;
    }
}