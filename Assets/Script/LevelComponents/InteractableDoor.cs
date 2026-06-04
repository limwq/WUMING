using UnityEngine;
using UnityEngine.SceneManagement; // Required for the failsafe scene loading
using System.Collections;

public class InteractableDoor : MonoBehaviour, IInteractable {
    [Header("Door Setup")]
    [Tooltip("Drag the hinge/pivot point of the door here. If left blank, it rotates the whole object.")]
    public Transform doorPivot;

    [Header("Rotation Settings")]
    public float openAngle = 90f;      // How far the door opens (degrees)
    public float openSpeed = 2f;       // How fast the door swings open

    // --- NEW: Scene Transition Settings ---
    [Header("Scene Transition")]
    [Tooltip("Check this if walking through the door should load a new level.")]
    public bool loadSceneOnOpen = false;
    [Tooltip("The EXACT name of the scene to load (e.g., 'Level_2').")]
    public string sceneToLoad = "";

    private bool hasOpened = false;
    private Quaternion closedRotation;
    private Quaternion targetOpenRotation;

    void Start() {
        // Fallback: If you forgot to assign a pivot, just use this object's transform
        if (doorPivot == null) {
            doorPivot = transform;
        }

        // Store the starting rotation
        closedRotation = doorPivot.rotation;

        // Calculate what the rotation will be when fully open (Rotating on the Y axis)
        targetOpenRotation = closedRotation * Quaternion.Euler(0, openAngle, 0);
    }

    public void Interact() {
        // Only allow interaction if it hasn't been opened yet
        if (!hasOpened) {
            hasOpened = true;
            StartCoroutine(OpenDoorRoutine());
            Debug.Log("<color=green>[Door] Opening door...</color>");
        }
    }

    public string GetInteractPrompt() {
        // If the door is already open, return an empty string so the UI prompt hides
        if (hasOpened) {
            return "";
        }

        // --- UPDATED: Dynamic UI text! ---
        if (loadSceneOnOpen) {
            return "Enter";
        }
        return "Open Door";
    }

    private IEnumerator OpenDoorRoutine() {
        float time = 0;

        if (AudioManager.Instance != null) {
            AudioManager.Instance.PlaySFX3D("DoorOpen", transform.position, 1f);
        }

        // Smoothly rotate from closed to open over time
        while (time < 1f) {
            time += Time.deltaTime * openSpeed;
            doorPivot.rotation = Quaternion.Slerp(closedRotation, targetOpenRotation, time);
            yield return null; // Wait for the next frame
        }

        // Snap precisely to the final angle just to be perfectly mathematically aligned
        doorPivot.rotation = targetOpenRotation;

        // --- NEW: Trigger the scene transition after the door fully opens! ---
        if (loadSceneOnOpen && !string.IsNullOrEmpty(sceneToLoad)) {
            LoadNextScene();
        }
    }

    // --- NEW: Helper method to safely trigger your master Scene Manager ---
    private void LoadNextScene() {
        Debug.Log($"<color=cyan>[InteractableDoor] Transitioning to scene: {sceneToLoad}</color>");

        if (GameSceneManager.Instance != null) {
            // Use the AAA fader you built!
            GameSceneManager.Instance.FadeScene(sceneToLoad);
        } else {
            // Failsafe just in case you are testing this room without your Bootstrapper
            SceneManager.LoadScene(sceneToLoad);
        }
    }
}