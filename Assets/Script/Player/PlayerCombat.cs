using UnityEngine;
using System.Collections;

public class PlayerCombat : MonoBehaviour {
    public bool IsAttacking { get; private set; }

    [Header("Weapon Settings")]
    [SerializeField] private DamageHitbox weaponHitbox;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private int comboStep = 0;
    private bool inputQueued = false;

    private Animator anim;
    private Camera mainCamera;

    private PlayerController playerController;
    // --- NEW: References to other states ---
    private PlayerDefense playerDefense;
    private PlayerRangeAttack playerRangeAttack;

    void Awake() {
        anim = GetComponentInChildren<Animator>();
        mainCamera = Camera.main;

        playerController = GetComponent<PlayerController>();
        playerDefense = GetComponent<PlayerDefense>();
        playerRangeAttack = GetComponent<PlayerRangeAttack>();
    }

    void Update() {
        if (Time.timeScale == 0f) return;

        if (playerController != null && playerController.IsDashing) {
            if (IsAttacking) {
                CancelAttack();
            }
            return;
        }

        // Input Listener
        if (Input.GetButtonDown("Fire1")) {

            // --- NEW: Cancel other actions before attacking! ---
            if (playerDefense != null && playerDefense.IsBlocking) {
                playerDefense.StopBlocking(true);
            }
            if (playerRangeAttack != null && playerRangeAttack.IsCharging) {
                playerRangeAttack.CancelCharge();
            }

            if (!IsAttacking) {
                if (debugLogs) Debug.Log($"[COMBAT] Click received. Starting Combo Step 1.");
                StartCoroutine(PerformCombo(1));
            } else {
                if (comboStep < 3) {
                    inputQueued = true;
                    if (debugLogs) Debug.Log($"[COMBAT] Click received mid-attack. Input QUEUED for Step {comboStep + 1}.");
                } else {
                    if (debugLogs) Debug.Log($"[COMBAT] Click ignored. Already at Max Combo (3).");
                }
            }
        }
    }

    IEnumerator PerformCombo(int step) {
        IsAttacking = true;
        comboStep = step;
        inputQueued = false;

        RotateToCameraView();

        if (anim != null) anim.SetTrigger("Attack" + comboStep);

        // Give the Animator exactly one frame to start transitioning
        yield return null;

        float clipLength = 1f; // Default safety

        if (anim.IsInTransition(0)) {
            clipLength = anim.GetNextAnimatorStateInfo(0).length;
        } else {
            clipLength = anim.GetCurrentAnimatorStateInfo(0).length;
        }

        // --- THE FIX: Clamp the max length! ---
        // If it accidentally grabs the Idle animation, force it back to 1 second
        if (clipLength <= 0f || clipLength > 1.5f) {
            clipLength = 1.0f;
        }

        // Play the sound at 30% of the swing, check for combo at 80% of the swing
        float soundWaitTime = clipLength * 0.3f;
        float comboWaitTime = clipLength * 0.5f; // 0.3 + 0.5 = 80% total

        yield return new WaitForSeconds(soundWaitTime);

        if (AudioManager.Instance != null) {
            AudioManager.Instance.PlaySFX3D("Sword_Swing", transform.position, 1f, 0.1f);
        }

        yield return new WaitForSeconds(comboWaitTime);

        // We are now at 80% of the animation. The sword is definitely OUT of the enemy!
        if (inputQueued && comboStep < 3) {
            StartCoroutine(PerformCombo(comboStep + 1));
        } else {
            // Wait the final 20% to let the animation follow-through completely
            yield return new WaitForSeconds(clipLength * 0.2f);
            ResetCombo();
        }
    }

    void RotateToCameraView() {
        Vector3 camForward = mainCamera.transform.forward;
        camForward.y = 0;
        camForward.Normalize();

        if (camForward != Vector3.zero) {
            transform.forward = camForward;
        }
    }

    void ResetCombo() {
        if (debugLogs) Debug.Log("[COMBAT] ResetCombo called. IsAttacking = false.");
        IsAttacking = false;
        comboStep = 0;
        inputQueued = false;

        if (anim != null) {
            anim.ResetTrigger("Attack1");
            anim.ResetTrigger("Attack2");
            anim.ResetTrigger("Attack3");
        }

        CloseHitbox();
    }

    public void CancelAttack() {
        StopAllCoroutines();
        ResetCombo();
        CloseHitbox();
    }

    public void ForceReset() {
        Debug.LogError("[COMBAT] Force Reset Triggered!");
        CancelAttack();
    }

    public void CloseHitbox() {
        if (weaponHitbox != null) {
            weaponHitbox.DisableHitbox();
        }
    }

    public void ShowWeaponMeshOnly() {
        if (!IsAttacking) return;

        if (weaponHitbox != null) {
            weaponHitbox.ShowWeaponMeshOnly();
        }
    }

    public void OpenHitbox() {
        if (!IsAttacking) return;

        if (weaponHitbox != null) {
            weaponHitbox.EnableHitbox();
        }
    }
}