using UnityEngine;
using System.Collections;

public class PlayerDefense : MonoBehaviour {

    public bool IsBlocking { get; private set; }

    [Header("Defense Stats")]
    [SerializeField] private float blockStaminaCost = 20f;
    [SerializeField] private float perfectBlockWindow = 0.25f;

    [Header("Animation Settings")]
    [SerializeField] private float raiseAnimationTime = 0.15f;
    [SerializeField] private float lowerAnimationTime = 0.25f;

    [Header("Perfect Block Rewards")]
    [SerializeField] private float staminaReward = 40f;
    [SerializeField] private float shockwaveRadius = 5f;
    [SerializeField] private int shockwaveDamage = 25;
    [SerializeField] private GameObject shockwaveVFX;
    [SerializeField] private GameObject ShockWavePos;

    [Header("Visuals")]
    [Tooltip("The outside half-transparent big sphere shield.")]
    public GameObject shieldVisual;

    [Tooltip("The Bagua mirror held in the player's hand.")]
    public GameObject baguaMirrorVisual;

    private PlayerCombat playerCombat;
    private PlayerStamina playerStamina;
    private PlayerController playerController;
    // --- NEW: Reference to Ranged Magic ---
    private PlayerRangeAttack playerRangeAttack;

    private Animator anim;
    private float currentBlockTimer = 0f;

    private Coroutine blockRoutine;

    private bool blockInterrupted = false;

    void Awake() {
        playerCombat = GetComponent<PlayerCombat>();
        playerStamina = GetComponent<PlayerStamina>();
        playerController = GetComponent<PlayerController>();
        playerRangeAttack = GetComponent<PlayerRangeAttack>();
        anim = GetComponentInChildren<Animator>();
    }

    void Start() {
        HideShieldMesh();
    }

    void Update() {
        if (playerController != null && playerController.IsDashing) {
            if (IsBlocking) StopBlocking();
            return;
        }

        // --- THE FIX: Check for KeyUp FIRST to reset the interrupt flag! ---
        if (Input.GetKeyUp(KeyCode.E)) {
            StopBlocking();
            blockInterrupted = false; // They let go, they are allowed to block again!
        }

        // --- THE FIX: Only block if we haven't been interrupted ---
        if (Input.GetKey(KeyCode.E) && !blockInterrupted) {
            
            if (playerCombat != null && playerCombat.IsAttacking) {
                playerCombat.CancelAttack();
            }
            if (playerRangeAttack != null && playerRangeAttack.IsCharging) {
                playerRangeAttack.CancelCharge();
            }

            if (CanBlock()) {
                StartBlocking();

                if (currentBlockTimer > 0) currentBlockTimer -= Time.deltaTime;

                if (playerStamina != null) {
                    bool hasStamina = playerStamina.DrainStamina(blockStaminaCost);
                    if (!hasStamina) StopBlocking();
                }
            } else {
                StopBlocking();
            }
        } 
    }

    public void ShowShieldMesh() {
        if (shieldVisual != null) {
            Renderer[] renderers = shieldVisual.GetComponentsInChildren<Renderer>();
            foreach (Renderer r in renderers) {
                r.enabled = true;
            }
        }

        if (baguaMirrorVisual != null) {
            Renderer[] renderers = baguaMirrorVisual.GetComponentsInChildren<Renderer>();
            foreach (Renderer r in renderers) {
                r.enabled = true;
            }
        }
    }

    public void HideShieldMesh() {
        if (shieldVisual != null) {
            Renderer[] renderers = shieldVisual.GetComponentsInChildren<Renderer>();
            foreach (Renderer r in renderers) {
                r.enabled = false;
            }
        }

        if (baguaMirrorVisual != null) {
            Renderer[] renderers = baguaMirrorVisual.GetComponentsInChildren<Renderer>();
            foreach (Renderer r in renderers) {
                r.enabled = false;
            }
        }
    }

    public bool IsPerfectBlocking() {
        return IsBlocking && currentBlockTimer > 0;
    }

    public void TriggerPerfectBlock(bool isMelee, Vector3 impactPoint = default) {
        Debug.Log("<color=cyan>PERFECT BLOCK!</color>");

        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX3D("Player_PerfectBlock", transform.position, 0.5f, 0f);

        if (impactPoint == default) {
            impactPoint = baguaMirrorVisual != null ? baguaMirrorVisual.transform.position : transform.position + (Vector3.up * 1.5f);
        }

        if (playerStamina != null) {
            playerStamina.GainStamina(staminaReward);
        }

        if (isMelee) {
            PerformShockwave();
        }

        Vector3 sparkDirection = (impactPoint - transform.position).normalized;
        if (VFXManager.Instance != null) {
            VFXManager.Instance.SpawnVFX("BlockSpark", impactPoint, Quaternion.LookRotation(sparkDirection));
            Debug.Log("BlockSpark spawned at " + impactPoint);
        }

        if (JuiceManager.Instance != null) {
            JuiceManager.Instance.ShakeCamera(0.5f);
            JuiceManager.Instance.TriggerHitStop(0.0f, 0.15f);
        }
    }

    void PerformShockwave() {
        if (shockwaveVFX != null) {
            Instantiate(shockwaveVFX, ShockWavePos.transform.position, Quaternion.identity);
        }

        Collider[] hitColliders = Physics.OverlapSphere(ShockWavePos.transform.position, shockwaveRadius);

        foreach (Collider hit in hitColliders) {
            if (hit.CompareTag("Enemy")) {
                EnemyBase enemyStats = hit.GetComponent<EnemyBase>();
                if (enemyStats != null) {
                    enemyStats.TakeDamage(shockwaveDamage);
                }
            }
        }
    }

    bool CanBlock() {
        if (playerStamina != null && playerStamina.CurrentStamina <= 0) return false;
        return true;
    }

    void StartBlocking() {
        if (IsBlocking) return;
        IsBlocking = true;
        currentBlockTimer = perfectBlockWindow;

        if (anim != null) {
            anim.SetBool("IsBlocking", true);
            anim.SetBool("IsHolding", false);
        }

        if (blockRoutine != null) StopCoroutine(blockRoutine);
        blockRoutine = StartCoroutine(TransitionToHold());
    }

    IEnumerator TransitionToHold() {
        yield return new WaitForSeconds(raiseAnimationTime);

        if (IsBlocking && anim != null) {
            anim.SetBool("IsHolding", true);
        }
    }

    public void StopBlocking(bool isForced = false) {
        if (!IsBlocking) return;

        // If another script called this, lock the shield until they let go of E
        if (isForced) blockInterrupted = true;

        if (blockRoutine != null) {
            StopCoroutine(blockRoutine);
            blockRoutine = null;
        }

        if (anim != null) {
            anim.SetBool("IsBlocking", false);
            anim.SetBool("IsHolding", false);
        }

        StartCoroutine(PutAwayRoutine());
    }

    IEnumerator PutAwayRoutine() {
        yield return new WaitForSeconds(lowerAnimationTime);

        HideShieldMesh();

        IsBlocking = false;
        currentBlockTimer = 0;
    }

    void OnDrawGizmosSelected() {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, shockwaveRadius);
    }
}