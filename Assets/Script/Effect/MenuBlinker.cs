using UnityEngine;
using System.Collections;

public class MenuBlinker : MonoBehaviour {
    [Header("Normal Blink Settings")]
    [Tooltip("Drag empty GameObjects here for the safe, background teleport locations.")]
    public Transform[] normalBlinkPoints;
    [Tooltip("How long it stays visible at a normal spot before blinking away")]
    public float timeBetweenBlinks = 5f;
    [Tooltip("The Audio tag to play for normal background teleports")]
    public string normalSfxTag = "Boss_Teleport_In";

    [Header("JUMPSCARE Settings")]
    [Tooltip("Drag the ONE empty GameObject placed right in front of the camera!")]
    public Transform jumpscarePoint;
    [Tooltip("Minimum seconds before a jumpscare can happen")]
    public float minJumpscareTime = 30f;
    [Tooltip("Maximum seconds before a jumpscare can happen")]
    public float maxJumpscareTime = 60f;
    [Tooltip("How long the jumpscare stays on screen before vanishing (Keep this short!)")]
    public float jumpscareLingerTime = 1.0f;
    [Tooltip("The Audio tag for the loud jumpscare scream")]
    public string jumpscareSfxTag = "Enemy_DieHowl";

    [Header("Visual Settings")]
    [Tooltip("Drag the 3D model/mesh of the object here so we can hide/show it")]
    public GameObject objectModel;
    [Tooltip("How long it stays invisible during the teleport")]
    public float invisibleDuration = 0.2f;
    [Tooltip("The VFX tag to spawn when it vanishes and reappears")]
    public string blinkVfxTag = "SpawnSmoke";

    private int currentIndex = 0;
    private float nextJumpscareTime;

    void Start() {
        if (normalBlinkPoints.Length > 0 && objectModel != null) {

            // --- THE FIX: Move the MODEL directly, ignoring the parent container! ---
            objectModel.transform.position = normalBlinkPoints[0].position;
            objectModel.transform.rotation = normalBlinkPoints[0].rotation;

            SetNextJumpscareTime();

            StartCoroutine(BlinkRoutine());
        } else {
            Debug.LogWarning("<color=yellow>[MenuBlinker] Missing Normal Blink Points or Object Model!</color>");
        }
    }

    private IEnumerator BlinkRoutine() {
        float currentWaitTime = timeBetweenBlinks;

        while (true) {
            yield return new WaitForSeconds(currentWaitTime);

            // ==========================================
            // 1. VANISH
            // ==========================================
            if (VFXManager.Instance != null && !string.IsNullOrEmpty(blinkVfxTag)) {
                // Spawn the VFX exactly where the model is standing, but shifted up 1.5 units
                Vector3 vfxPosition = objectModel.transform.position + (Vector3.up * 1.5f);
                VFXManager.Instance.SpawnVFX(blinkVfxTag, vfxPosition, objectModel.transform.rotation);
            }

            objectModel.SetActive(false);
            yield return new WaitForSeconds(invisibleDuration);

            // ==========================================
            // 2. CHOOSE DESTINATION
            // ==========================================
            if (jumpscarePoint != null && Time.time >= nextJumpscareTime) {
                // --- TRIGGER JUMPSCARE! ---
                objectModel.transform.position = jumpscarePoint.position;
                objectModel.transform.rotation = jumpscarePoint.rotation;

                SetNextJumpscareTime();
                currentWaitTime = jumpscareLingerTime;

                if (AudioManager.Instance != null && !string.IsNullOrEmpty(jumpscareSfxTag)) {
                    AudioManager.Instance.PlayUI(jumpscareSfxTag);
                }

                Debug.Log("<color=red>[MenuBlinker] BOO! Jumpscare Triggered!</color>");
            } else {
                // --- NORMAL RANDOM BLINK ---
                int newIndex = currentIndex;

                // Ensure we don't pick the exact same point twice in a row
                if (normalBlinkPoints.Length > 1) {
                    while (newIndex == currentIndex) {
                        newIndex = Random.Range(0, normalBlinkPoints.Length);
                    }
                }
                currentIndex = newIndex;

                // --- THE FIX: Snap the model perfectly to the point ---
                objectModel.transform.position = normalBlinkPoints[currentIndex].position;
                objectModel.transform.rotation = normalBlinkPoints[currentIndex].rotation;

                currentWaitTime = timeBetweenBlinks;

                if (AudioManager.Instance != null && !string.IsNullOrEmpty(normalSfxTag)) {
                    AudioManager.Instance.PlayUI(normalSfxTag);
                }

                // Debug log to verify it is picking different points!
                Debug.Log($"<color=cyan>[MenuBlinker] Teleported to Normal Point [{currentIndex}]</color>");
            }

            // ==========================================
            // 3. REAPPEAR
            // ==========================================
            if (VFXManager.Instance != null && !string.IsNullOrEmpty(blinkVfxTag)) {
                VFXManager.Instance.SpawnVFX(blinkVfxTag, objectModel.transform.position, objectModel.transform.rotation);
            }

            objectModel.SetActive(true);
        }
    }

    private void SetNextJumpscareTime() {
        nextJumpscareTime = Time.time + Random.Range(minJumpscareTime, maxJumpscareTime);
    }
}