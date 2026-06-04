using UnityEngine;
using System.Collections;

public class InteractableSlidingDoor : MonoBehaviour, IInteractable {
    [Header("Door Setup")]
    [Tooltip("The physical door mesh that will slide. Leave blank to move this entire object.")]
    public Transform doorPanel;

    [Header("Sliding Settings")]
    [Tooltip("How far the door slides to the right (Positive X). Use negative numbers to slide left.")]
    public float slideDistance = 2.5f;
    [Tooltip("How fast the door slides open.")]
    public float slideSpeed = 2f;

    [Header("Lock Status")]
    [Tooltip("Is the door locked at the start of the game?")]
    public bool isLocked = true;

    private bool hasOpened = false;
    private Vector3 closedPosition;
    private Vector3 targetOpenPosition;

    void Start() {
        if (doorPanel == null) {
            doorPanel = transform;
        }

        // Store the starting position
        closedPosition = doorPanel.localPosition;

        // Calculate the open position. Vector3.left is the local X axis.
        targetOpenPosition = closedPosition + (Vector3.left * slideDistance);
    }

    public void Interact() {
        // 1. Check if the door is locked
        if (isLocked) {
            Debug.Log("<color=red>[Metal Door] Access Denied. The door is locked.</color>");
            // (Optional) Play a "clunk" or error sound effect here
            return;
        }

        // 2. If unlocked and closed, open it!
        if (!hasOpened) {
            hasOpened = true;
            StartCoroutine(OpenDoorRoutine());
            Debug.Log("<color=green>[Metal Door] Sliding open...</color>");
        }
    }

    public string GetInteractPrompt() {
        if (hasOpened) return ""; // Hide the prompt if the door is already open

        if (isLocked) return "Door Locked"; // Show a locked message

        return "Open Metal Door"; // Show the open message if unlocked
    }

    // --- THE UNLOCK FUNCTION ---
    // You can call this from a Button, a Key Item, or when a Boss dies!
    public void UnlockDoor() {
        if (isLocked) {
            isLocked = false;
            Debug.Log("<color=yellow>[Metal Door] Lock disengaged!</color>");
        }
    }

    private IEnumerator OpenDoorRoutine() {
        float time = 0;

        // Smoothly slide from the closed position to the open position
        while (time < 1f) {
            time += Time.deltaTime * slideSpeed;
            doorPanel.localPosition = Vector3.Lerp(closedPosition, targetOpenPosition, time);
            yield return null;
        }

        // Snap precisely to the final position to ensure perfect alignment
        doorPanel.localPosition = targetOpenPosition;
    }
}