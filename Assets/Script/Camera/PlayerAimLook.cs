using UnityEngine;

public class PlayerAimLook : MonoBehaviour {
    [Header("Camera Target")]
    [Tooltip("Drag your CameraLookTarget here")]
    public Transform cameraLookTarget;

    [Header("Combat Reference")]
    public PlayerRangeAttack rangeAttackScript;
    public BossLockOn bossLockOnScript;

    [Header("Settings")]
    public float mouseSensitivity = 2f;
    public Vector2 pitchLimits = new Vector2(-40f, 60f);

    [Header("Movement Reference")]
    public PlayerController playerController;

    [Header("Smoothing")]
    public float smoothTime = 0.05f;
    private float xVelocity;
    private float yVelocity;
    private float currentXAxis;
    private float currentYAxis;

    private float xAxis;
    private float yAxis;

    // --- NEW: Track the height of the camera point! ---
    private float targetHeightOffset;

    void Start() {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (cameraLookTarget != null) {
            xAxis = cameraLookTarget.eulerAngles.y;

            // --- THE FIX: THE DECOUPLED GIMBAL ---
            // 1. Memorize how high up you placed the point in the prefab (e.g., at the neck)
            targetHeightOffset = cameraLookTarget.position.y - transform.position.y;

            // 2. Unparent it from the player entirely! 
            // Now the player can spin wildly, and the camera won't feel a thing.
            cameraLookTarget.SetParent(null);
        }
    }

    void Update() {
        bool isAiming = (rangeAttackScript != null && rangeAttackScript.IsCharging);
        bool isLockedOn = (bossLockOnScript != null && bossLockOnScript.isLockedOn);

        if (!isLockedOn || isAiming) {
            xAxis += Input.GetAxis("Mouse X") * mouseSensitivity;
            yAxis -= Input.GetAxis("Mouse Y") * mouseSensitivity;
            yAxis = Mathf.Clamp(yAxis, pitchLimits.x, pitchLimits.y);
        }
    }

    void LateUpdate() {
        if (cameraLookTarget == null) return;

        // --- THE FIX: Manually follow the player's position ---
        // Because it is no longer a child, we manually tell it to stick to our 
        // feet + the original height offset every frame.
        cameraLookTarget.position = transform.position + new Vector3(0, targetHeightOffset, 0);


        // --- THE REST OF YOUR LOGIC (Exactly the same as before) ---
        bool isAiming = (rangeAttackScript != null && rangeAttackScript.IsCharging);

        if (bossLockOnScript != null && bossLockOnScript.isLockedOn && !isAiming) {
            Transform mainCam = Camera.main.transform;

            xAxis = mainCam.eulerAngles.y;
            float pitch = mainCam.eulerAngles.x;
            if (pitch > 180f) pitch -= 360f;
            yAxis = pitch;

            currentXAxis = xAxis;
            currentYAxis = yAxis;

            cameraLookTarget.rotation = Quaternion.Euler(yAxis, xAxis, 0);

            if (bossLockOnScript.currentBoss != null && (playerController == null || !playerController.IsDashing)) {
                Vector3 dirToBoss = bossLockOnScript.currentBoss.position - transform.position;
                dirToBoss.y = 0;

                if (dirToBoss != Vector3.zero) {
                    Quaternion targetRotation = Quaternion.LookRotation(dirToBoss);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 15f);
                }
            }
            return;
        }

        currentXAxis = Mathf.SmoothDampAngle(currentXAxis, xAxis, ref xVelocity, smoothTime);
        currentYAxis = Mathf.SmoothDampAngle(currentYAxis, yAxis, ref yVelocity, smoothTime);

        Quaternion targetRotationFree = Quaternion.Euler(currentYAxis, currentXAxis, 0);
        cameraLookTarget.rotation = targetRotationFree;

        if (isAiming) {
            Quaternion targetRotationfix = Quaternion.Euler(0, xAxis, 0);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotationfix, Time.deltaTime * 15f);
        }
    }
}