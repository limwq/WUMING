using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour {
    [Header("Targets")]
    public Transform player;       // The Player object
    public Transform cameraTarget; // A specific point (e.g. Neck/Head) to look at

    [Header("Settings")]
    public float mouseSensitivity = 2f;
    public float distanceFromPlayer = 3f;
    public Vector2 pitchLimits = new Vector2(-10, 20); // Don't flip the camera upside down

    private float yaw;   // Horizontal rotation
    private float pitch; // Vertical rotation

    private void Start() {
        // Hide and lock the cursor to the center of the screen
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void LateUpdate() {
        if (!player) return;

        // 1. Get Mouse Input
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // 2. Calculate Rotation
        yaw += mouseX;
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, pitchLimits.x, pitchLimits.y); // Clamp vertical look

        // 3. Apply Rotation and Position
        Vector3 targetRotation = new Vector3(pitch, yaw, 0);
        transform.eulerAngles = targetRotation;

        // 4. Position camera behind the player based on rotation
        // We take the target position -> move back by distance -> Apply rotation
        Vector3 offset = new Vector3(0, 0, -distanceFromPlayer);
        transform.position = cameraTarget.position + Quaternion.Euler(pitch, yaw, 0) * offset;
    }
}