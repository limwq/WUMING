using UnityEngine;
using Unity.Cinemachine;

public class BossLockOn : MonoBehaviour {
    [Header("Camera Setup")]
    [SerializeField] private CinemachineCamera lockOnCam;
    [SerializeField] private CinemachineTargetGroup targetGroup;
    [Tooltip("Drag the empty LockOnMount child object here")]
    [SerializeField] private Transform cameraMount;

    [Header("Combat Reference")]
    [Tooltip("Drag your Player here so Lock-On knows when you are aiming!")]
    [SerializeField] private PlayerRangeAttack rangeAttackScript;
    [Header("Settings")]
    [SerializeField] private KeyCode lockOnKey = KeyCode.Mouse2;
    [Tooltip("How hard you have to move the mouse to break the lock-on")]
    [SerializeField] private float mouseUnlockThreshold = 2.0f;

    public bool isLockedOn = false;
    public Transform currentBoss;

    void Update() {
        if (Input.GetKeyDown(lockOnKey)) {
            ToggleLockOn();
        }

        // --- NEW: Keep the invisible mount staring at the boss! ---
        if (isLockedOn && currentBoss != null) {

            // THE FIX: Check if the player flicks the mouse to break the lock!
            float mouseFlick = Mathf.Abs(Input.GetAxis("Mouse X")) + Mathf.Abs(Input.GetAxis("Mouse Y"));
            if (mouseFlick > mouseUnlockThreshold) {
                ToggleLockOn(); // Turn it off!
                return; // Stop running the rest of the camera math this frame
            }

            // Temporarily drop priority if the player is aiming!
            if (rangeAttackScript != null && rangeAttackScript.IsCharging) {
                lockOnCam.Priority = 0;
            } else {
                lockOnCam.Priority = 20;
            }

            if (cameraMount != null) {
                Vector3 dirToBoss = currentBoss.position - cameraMount.position;
                dirToBoss.y = 0;
                if (dirToBoss != Vector3.zero) {
                    cameraMount.forward = dirToBoss;
                }
            }
        }
    }

    void ToggleLockOn() {
        isLockedOn = !isLockedOn;

        if (isLockedOn) {
            BossBrain bossScript = FindFirstObjectByType<BossBrain>();

            if (bossScript != null) {
                currentBoss = bossScript.transform;

                if (cameraMount != null) {
                    Vector3 dirToBoss = currentBoss.position - cameraMount.position;
                    dirToBoss.y = 0;
                    if (dirToBoss != Vector3.zero) {
                        cameraMount.forward = dirToBoss;
                    }
                }

                targetGroup.AddMember(currentBoss, 1f, 2f);
                lockOnCam.Priority = 20;
            } else {
                isLockedOn = false;
            }
        } else {
            lockOnCam.Priority = 0;
            if (currentBoss != null) {
                targetGroup.RemoveMember(currentBoss);
                currentBoss = null;
            }
        }
    }
}