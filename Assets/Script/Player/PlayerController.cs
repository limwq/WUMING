using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour {
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField, Range(0.01f, 1f)] private float chargeSpeedMultiplier = 0.5f;

    [Header("Animation Smoothing")]
    [SerializeField] private float animSmoothTime = 0.1f;
    private float currentVelX;
    private float currentVelZ;

    [Header("Slope & Grounding")]
    [SerializeField] private float maxSlopeAngle = 45f;
    [SerializeField] private float gravityMultiplier = 5f;
    [SerializeField] private float raycastDistance = 1.2f;
    [SerializeField] private LayerMask groundLayer;

    private RaycastHit slopeHit;

    [Header("Dodge Settings")]
    [SerializeField] private float dashSpeed = 15f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashCooldown = 1f;

    [Header("Combat References")]
    public BossLockOn lockOnScript;

    // --- Audio Settings ---
    [Header("Audio")]
    [Tooltip("Drag the AudioSource attached to the Player that handles looping footsteps here")]
    [SerializeField] private AudioSource footstepAudioSource;

    // State Flags
    public bool IsDashing { get; private set; }
    private bool canDash = true;
    private Vector3 currentMoveDir;
    private Vector3 knockbackVelocity = Vector3.zero;
    private Rigidbody rb;
    private PlayerDefense playerDefense;
    private PlayerCombat playerCombat;
    private PlayerRangeAttack playerRangeAttack;

    private PlayerAimLook playerAimLook;
    private Camera mainCamera;
    private Animator anim;

    void Awake() {
        rb = GetComponent<Rigidbody>();
        playerDefense = GetComponent<PlayerDefense>();
        playerCombat = GetComponent<PlayerCombat>();
        playerRangeAttack = GetComponent<PlayerRangeAttack>();
        playerAimLook = GetComponent<PlayerAimLook>();
        mainCamera = Camera.main;
        anim = GetComponentInChildren<Animator>();

        rb.useGravity = true;
    }

    void Start() {
        if (SaveManager.Instance != null) {
            SaveManager.GameData data = SaveManager.Instance.LoadGame();
            if (data != null && data.sceneName == UnityEngine.SceneManagement.SceneManager.GetActiveScene().name) {
                transform.position = data.playerPosition;
                Debug.Log("<color=green>[Player] Spawned at Checkpoint!</color>");
            }
        }
    }

    void Update() {
        if (Input.GetMouseButtonDown(1) && canDash && !IsDashing) {
            StartCoroutine(PerformDodge());
        }

        // --- THE FIX: CALCULATE DIRECTION IN UPDATE! ---
        if (!IsDashing) {
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");

            Vector3 camForward = mainCamera.transform.forward;
            Vector3 camRight = mainCamera.transform.right;
            camForward.y = 0; camRight.y = 0;
            camForward.Normalize(); camRight.Normalize();

            currentMoveDir = (camForward * v + camRight * h).normalized;
        }
    }

    void FixedUpdate() {
        if (knockbackVelocity.magnitude > 0.1f) {
            rb.linearVelocity = new Vector3(knockbackVelocity.x, rb.linearVelocity.y, knockbackVelocity.z);
            knockbackVelocity = Vector3.Lerp(knockbackVelocity, Vector3.zero, 5f * Time.fixedDeltaTime);
            UpdateAnimatorStop();
            return;
        }

        if (IsDashing) return;

        if (playerDefense != null && playerDefense.IsBlocking) {
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
            UpdateAnimatorStop();
            return;
        }

        if (playerCombat != null && playerCombat.IsAttacking) {
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
            UpdateAnimatorStop();
            return;
        }

        MovePlayerRelativeToCamera();

        if (!IsDashing) {
            rb.AddForce(Vector3.down * gravityMultiplier * 10f, ForceMode.Acceleration);
        }
    }

    public void ApplyKnockback(Vector3 force) {
        knockbackVelocity = force;
        IsDashing = false;

        if (playerCombat != null && playerCombat.IsAttacking) playerCombat.CancelAttack();
        if (playerRangeAttack != null && playerRangeAttack.IsCharging) playerRangeAttack.CancelCharge();
    }

    void MovePlayerRelativeToCamera() {
        // --- WE NOW USE currentMoveDir INSTEAD OF GETTING INPUTS HERE! ---

        float currentSpeed = moveSpeed;
        bool isCharging = playerRangeAttack != null && playerRangeAttack.IsCharging;

        if (isCharging) currentSpeed *= chargeSpeedMultiplier;

        if (lockOnScript == null || !lockOnScript.isLockedOn) {
            if (currentMoveDir.magnitude > 0.1f && !isCharging) {
                Quaternion targetRotation = Quaternion.LookRotation(currentMoveDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
            }
        }

        // --- PHYSICS MOVEMENT & FOOTSTEP AUDIO ---
        if (currentMoveDir.magnitude > 0.1f) {
            if (OnSlope()) {
                Vector3 slopeDir = GetSlopeMoveDirection(currentMoveDir);
                rb.linearVelocity = slopeDir * currentSpeed;
            } else {
                rb.linearVelocity = new Vector3(currentMoveDir.x * currentSpeed, rb.linearVelocity.y, currentMoveDir.z * currentSpeed);
            }

            if (footstepAudioSource != null) {
                footstepAudioSource.pitch = isCharging ? (1.2f * chargeSpeedMultiplier) : 1.2f;
                if (!footstepAudioSource.isPlaying) footstepAudioSource.Play();
            }
        } else {
            rb.linearVelocity = new Vector3(
                Mathf.Lerp(rb.linearVelocity.x, 0, 10f * Time.fixedDeltaTime),
                rb.linearVelocity.y,
                Mathf.Lerp(rb.linearVelocity.z, 0, 10f * Time.fixedDeltaTime));

            if (footstepAudioSource != null && footstepAudioSource.isPlaying) {
                footstepAudioSource.Stop();
            }
        }

        if (anim != null) {
            anim.SetBool("IsChargingRanged", isCharging);

            float targetAnimRatio = 0f;
            if (currentMoveDir.magnitude > 0.1f) {
                targetAnimRatio = isCharging ? chargeSpeedMultiplier : 1.0f;
            }

            Vector3 localMovement = transform.InverseTransformDirection(currentMoveDir);
            float targetX = localMovement.x * targetAnimRatio;
            float targetZ = localMovement.z * targetAnimRatio;

            currentVelX = Mathf.Lerp(currentVelX, targetX, Time.fixedDeltaTime * (1f / animSmoothTime));
            currentVelZ = Mathf.Lerp(currentVelZ, targetZ, Time.fixedDeltaTime * (1f / animSmoothTime));

            anim.SetFloat("VelocityX", currentVelX);
            anim.SetFloat("VelocityZ", currentVelZ);
        }
    }

    private bool OnSlope() {
        if (Physics.Raycast(transform.position, Vector3.down, out slopeHit, raycastDistance, groundLayer)) {
            float angle = Vector3.Angle(Vector3.up, slopeHit.normal);
            return angle < maxSlopeAngle && angle != 0;
        }
        return false;
    }

    private Vector3 GetSlopeMoveDirection(Vector3 direction) {
        return Vector3.ProjectOnPlane(direction, slopeHit.normal).normalized;
    }

    void UpdateAnimatorStop() {
        if (anim != null) {
            currentVelX = 0f;
            currentVelZ = 0f;
            anim.SetFloat("VelocityX", 0f);
            anim.SetFloat("VelocityZ", 0f);
            anim.SetBool("IsChargingRanged", false);
        }

        if (footstepAudioSource != null && footstepAudioSource.isPlaying) {
            footstepAudioSource.Stop();
        }
    }

    IEnumerator PerformDodge() {
        if (playerCombat != null && playerCombat.IsAttacking) playerCombat.CancelAttack();
        if (playerRangeAttack != null && playerRangeAttack.IsCharging) playerRangeAttack.CancelCharge();
        if (playerDefense != null && playerDefense.IsBlocking) playerDefense.StopBlocking(true);

        IsDashing = true;
        canDash = false;

        if (footstepAudioSource != null && footstepAudioSource.isPlaying) {
            footstepAudioSource.Stop();
        }

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector3 camForward = mainCamera.transform.forward;
        Vector3 camRight = mainCamera.transform.right;

        camForward.y = 0; camRight.y = 0;
        camForward.Normalize(); camRight.Normalize();

        Vector3 inputDir = (camForward * v + camRight * h).normalized;
        Vector3 dashDirection = inputDir.magnitude > 0.1f ? inputDir : transform.forward;

        if (dashDirection != Vector3.zero) {
            transform.forward = dashDirection;
        }

        if (VFXManager.Instance != null) {
            VFXManager.Instance.SpawnVFX("DashDust", transform.position, Quaternion.LookRotation(-dashDirection));
        }

        if (anim != null) anim.SetTrigger("Dodge");

        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX3D("Player_Dash", transform.position);

        float startTime = Time.time;
        while (Time.time < startTime + dashDuration) {

            // --- THE FIX: Safely abort without killing momentum! ---
            if (knockbackVelocity.magnitude > 0.1f) {
                IsDashing = false;
                yield return new WaitForSeconds(dashCooldown);
                canDash = true;

                // CRITICAL: Exit the coroutine completely so we don't zero out the velocity below!
                yield break;
            }

            rb.MovePosition(transform.position + dashDirection * dashSpeed * Time.fixedDeltaTime);
            yield return new WaitForFixedUpdate();
        }

        rb.linearVelocity = Vector3.zero;
        IsDashing = false;

        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }

    private void OnDrawGizmos() {
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, Vector3.down * raycastDistance);
    }

    public void SetPlayerControl(bool hasControl) {
        this.enabled = hasControl;

        if (playerCombat != null) playerCombat.enabled = hasControl;
        if (playerDefense != null) playerDefense.enabled = hasControl;
        if (playerRangeAttack != null) playerRangeAttack.enabled = hasControl;
        if (playerAimLook != null) playerAimLook.enabled = hasControl;

        if (!hasControl) {
            // --- THE FIX: Nuke active dodges and reset the failsafes! ---
            StopAllCoroutines();
            IsDashing = false;
            canDash = true;

            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
            UpdateAnimatorStop();

            if (footstepAudioSource != null && footstepAudioSource.isPlaying) {
                footstepAudioSource.Stop();
            }

            if (playerCombat != null) playerCombat.CancelAttack();
            if (playerRangeAttack != null && playerRangeAttack.IsCharging) playerRangeAttack.CancelCharge();
            if (playerDefense != null && playerDefense.IsBlocking) playerDefense.StopBlocking(true);
        }
    }
}