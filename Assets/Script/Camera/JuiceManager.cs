using UnityEngine;
using Unity.Cinemachine; // --- NEW: The Unity 6 Cinemachine 3.x Namespace! ---
using System.Collections;

public class JuiceManager : MonoBehaviour {
    public static JuiceManager Instance { get; private set; }

    [Header("Camera Shake")]
    [Tooltip("Requires a Cinemachine Impulse Source component on this object")]
    public CinemachineImpulseSource impulseSource;

    private float originalFixedDeltaTime;

    void Awake() {
        if (Instance == null) {
            Instance = this;
            originalFixedDeltaTime = Time.fixedDeltaTime;
            
            // --- THE FIX: Tell Unity not to destroy this object when loading a new scene! ---
            DontDestroyOnLoad(gameObject); 
        } else {
            Destroy(gameObject);
        }
    }

    // --- 1. THE CAMERA SHAKE ---
    public void ShakeCamera(float intensity = 0.5f) {
        if (impulseSource != null) {
            // --- NEW: The updated CM3 API for generating force ---
            impulseSource.GenerateImpulseWithForce(intensity);
        }
    }

    // --- 2. THE BOSS INTERRUPT SLOW-MO ---
    public void TriggerHitStop(float slowTimeScale = 0.1f, float durationRealtime = 0.2f) {
        StartCoroutine(HitStopRoutine(slowTimeScale, durationRealtime));
    }

    private IEnumerator HitStopRoutine(float targetTimeScale, float duration) {
        if (Time.timeScale < 1f) yield break;

        Time.timeScale = targetTimeScale;
        Time.fixedDeltaTime = originalFixedDeltaTime * targetTimeScale;

        yield return new WaitForSecondsRealtime(duration);

        Time.timeScale = 1f;
        Time.fixedDeltaTime = originalFixedDeltaTime;
    }
}