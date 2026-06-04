using UnityEngine;

public class ControlPlaneTrigger : MonoBehaviour {
    [Header("Sprite References")]
    [Tooltip("Drag the GameObject holding your SpriteRenderer here")]
    [SerializeField] private SpriteRenderer controlSprite;

    [Header("Trigger Settings")]
    [Tooltip("Make sure your Player GameObject has this exact tag!")]
    [SerializeField] private string playerTag = "Player";

    [Header("Audio Settings")]
    [SerializeField] private string openSfxTag = "Testing_ButtonClick";
    [SerializeField] private string closeSfxTag = "Testing_ButtonClick";

    private bool isOpen = false;

    void Start() {
        // Ensure the sprite starts turned off
        if (controlSprite != null) {
            controlSprite.enabled = false;
        }
    }

    // --- Trigger Physics Events ---
    private void OnTriggerEnter(Collider other) {
        // Only open if the PLAYER walks into the box (ignore enemies/bullets)
        if (!isOpen && other.CompareTag(playerTag)) {
            OpenPlane();
        }
    }

    private void OnTriggerExit(Collider other) {
        // Automatically close when the PLAYER walks out of the box
        if (isOpen && other.CompareTag(playerTag)) {
            ClosePlane();
        }
    }

    // --- Core Logic ---
    private void OpenPlane() {
        isOpen = true;

        // Turn the SpriteRenderer ON
        if (controlSprite != null) controlSprite.enabled = true;

        if (AudioManager.Instance != null && !string.IsNullOrEmpty(openSfxTag)) {
            AudioManager.Instance.PlayUI(openSfxTag);
        }
    }

    private void ClosePlane() {
        isOpen = false;

        // Turn the SpriteRenderer OFF
        if (controlSprite != null) controlSprite.enabled = false;

        if (AudioManager.Instance != null && !string.IsNullOrEmpty(closeSfxTag)) {
            AudioManager.Instance.PlayUI(closeSfxTag);
        }
    }
}