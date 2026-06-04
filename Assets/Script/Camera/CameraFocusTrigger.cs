using UnityEngine;
using Unity.Cinemachine;
using System.Collections;

[RequireComponent(typeof(BoxCollider))]
public class CameraFocusTrigger : MonoBehaviour {

    [Header("Camera Settings")]
    [Tooltip("The Cinemachine Camera that is aiming at the specific place")]
    [SerializeField] private CinemachineCamera focusCamera;

    [Tooltip("How long to look at the object before giving control back?")]
    [SerializeField] private float focusDuration = 3.0f;

    [Tooltip("The priority to set the camera to so it overrides the Player camera")]
    [SerializeField] private int activePriority = 20;

    // --- NEW: Dynamic Blend Controls ---
    [Header("Cinematic Blending")]
    [Tooltip("How many seconds it takes to pan TO the focus point, and back TO the player")]
    [SerializeField] private float cinematicBlendTime = 2.0f;

    [Header("References")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private GameObject mainHUD;

    private bool hasTriggered = false;

    private void Start() {
        if (focusCamera != null) {
            focusCamera.Priority = 0;
        }

        Collider col = GetComponent<Collider>();
        if (col != null) {
            col.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other) {
        if (other.CompareTag("Player") && !hasTriggered) {
            hasTriggered = true;
            StartCoroutine(FocusSequence());
        }
    }

    private IEnumerator FocusSequence() {
        Debug.Log("<color=cyan>[CameraFocus] Triggered! Stealing camera and locking player.</color>");

        // 1. LOCK THE PLAYER & HIDE UI
        if (playerController != null) playerController.SetPlayerControl(false);
        if (mainHUD != null) mainHUD.SetActive(false);

        // --- THE FIX: HIJACK THE CINEMACHINE BRAIN SAFELY ---
        CinemachineBrain brain = Camera.main.GetComponent<CinemachineBrain>();
        CinemachineBlendDefinition originalBlend = default; // Safely initialize an empty struct

        if (brain != null) {
            // Save the exact settings you had before
            originalBlend = brain.DefaultBlend;

            // Create a temporary copy, change ONLY the time, and apply it
            var slowBlend = originalBlend;
            slowBlend.Time = cinematicBlendTime;
            brain.DefaultBlend = slowBlend;
        }

        // 2. BOOST THE CAMERA PRIORITY (Starts the 2-second pan OUT)
        if (focusCamera != null) {
            focusCamera.Priority = activePriority;
        }

        // Wait for the camera to actually arrive at the target
        yield return new WaitForSeconds(cinematicBlendTime);

        // 3. WAIT FOR THE DURATION (Stare at the object)
        if (focusDuration > 0) {
            yield return new WaitForSeconds(focusDuration);

            // 4. LOWER PRIORITY TO RETURN TO PLAYER (Starts the 2-second pan BACK)
            if (focusCamera != null) {
                focusCamera.Priority = 0;
            }

            // Wait for the camera to physically travel back to the player's shoulder
            yield return new WaitForSeconds(cinematicBlendTime);

            // --- THE FIX: RESTORE THE BRAIN ---
            if (brain != null) {
                // Put the exact original blend back, restoring your snappy 0.15s aiming!
                brain.DefaultBlend = originalBlend;
            }

            // 5. UNLOCK PLAYER & SHOW UI
            if (playerController != null) playerController.SetPlayerControl(true);
            if (mainHUD != null) mainHUD.SetActive(true);

            Debug.Log("<color=cyan>[CameraFocus] Sequence finished. Player control restored.</color>");
        }
    }
}