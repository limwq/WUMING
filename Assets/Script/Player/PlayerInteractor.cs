using UnityEngine;
using TMPro; // --- NEW: Required for updating TextMeshPro UI! ---

public class PlayerInteractor : MonoBehaviour {
    [Header("Interaction Settings")]
    public float interactRange = 2f;
    public LayerMask interactableLayer;

    [Header("UI Settings")]
    [Tooltip("Drag the parent GameObject containing your interaction UI here")]
    public GameObject interactPromptUI;

    // --- NEW: The actual text component we will change ---
    [Tooltip("Drag your TextMeshPro text element here so the script can change the words")]
    public TextMeshProUGUI promptText;

    private IInteractable currentTarget;

    void Update() {
        // 1. Scan for interactables every frame
        FindClosestInteractable();

        // 2. Update the UI dynamically!
        if (currentTarget != null) {
            // Ask the target what its specific text should be
            string promptMessage = currentTarget.GetInteractPrompt();

            // If the object returns an empty string (e.g., the door is already open), hide the UI
            if (string.IsNullOrEmpty(promptMessage)) {
                if (interactPromptUI != null) interactPromptUI.SetActive(false);
            } else {
                // Otherwise, show the UI and update the text!
                if (interactPromptUI != null) interactPromptUI.SetActive(true);
                if (promptText != null) {
                    promptText.text = "[F] " + promptMessage; // Output: "[F] Open Door" or "[F] Begin Unsealing"
                }
            }
        } else {
            // No target in range, turn the UI off
            if (interactPromptUI != null) interactPromptUI.SetActive(false);
        }

        // 3. Pressing 'F' to interact ONLY if we have a valid target
        if (Input.GetKeyDown(KeyCode.F) && currentTarget != null) {
            currentTarget.Interact();
        }
    }

    void FindClosestInteractable() {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, interactRange, interactableLayer);

        IInteractable closestInteractable = null;
        float closestDistance = float.MaxValue;

        foreach (Collider hit in hitColliders) {
            IInteractable interactable = hit.GetComponent<IInteractable>();
            if (interactable != null) {
                float distance = Vector3.Distance(transform.position, hit.transform.position);
                if (distance < closestDistance) {
                    closestDistance = distance;
                    closestInteractable = interactable;
                }
            }
        }

        currentTarget = closestInteractable;
    }

    private void OnDrawGizmosSelected() {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}