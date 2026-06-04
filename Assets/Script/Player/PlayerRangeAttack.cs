using UnityEngine;
using Unity.Cinemachine;

public class PlayerRangeAttack : MonoBehaviour {

    [Header("Attack Settings")]
    [SerializeField] private float chargeTimeRequired = 1.5f;

    [Header("Cooldowns")]
    [SerializeField] private float normalCooldown = 2f;
    [SerializeField] private float chargedCooldown = 5f;

    [Header("Projectiles (Pool Tags)")]
    [Tooltip("Must match the exact tag in the ProjectilePooler")]
    [SerializeField] private string instantProjectileTag = "PlayerNormal";
    [Tooltip("Must match the exact tag in the ProjectilePooler")]
    [SerializeField] private string chargedProjectileTag = "PlayerCharge";
    [SerializeField] private Transform firePoint;

    [Header("Targeting")]
    [SerializeField] private LayerMask aimMask;

    // --- TALISMAN VISUALS & VFX ---
    [Header("Talisman Visuals")]
    [Tooltip("The actual paper talisman mesh in the player's hand")]
    [SerializeField] private GameObject talismanMesh;

    [Tooltip("The Ring Prefab to spawn when max charge is hit")]
    [SerializeField] private GameObject maxChargeRingPrefab;

    [Tooltip("The roaring fire & smoke that grows while charging")]
    [SerializeField] private ParticleSystem fullyOnFireVFX;

    // --- NEW: Talisman Audio ---
    [Header("Audio Settings")]
    [Tooltip("Drag the AudioSource attached to the player's hand/talisman here")]
    [SerializeField] private AudioSource chargeAudioSource;
    [Tooltip("The maximum volume the fire should reach at full charge")]
    [SerializeField] private float maxChargeVolume = 1.0f;

    private Animator anim;
    private PlayerController playerController;
    private PlayerDefense playerDefense;
    private PlayerCombat playerCombat;
    private Camera mainCam;
    private CinemachineImpulseSource impulseSource;

    private float currentChargeTimer = 0f;
    private float currentCooldownTimer = 0f;

    // State Tracking
    private bool hasPlayedMaxChargePing = false;
    private Vector3 originalFireScale;

    public bool IsCharging { get; private set; }

    void Awake() {
        anim = GetComponentInChildren<Animator>();
        playerController = GetComponent<PlayerController>();
        playerDefense = GetComponent<PlayerDefense>();
        playerCombat = GetComponent<PlayerCombat>();
        mainCam = Camera.main;
        impulseSource = GetComponent<CinemachineImpulseSource>();

        if (fullyOnFireVFX != null) {
            originalFireScale = fullyOnFireVFX.transform.localScale;
        }
    }

    void Start() {
        HideTalisman();
    }

    void Update() {
        if (currentCooldownTimer > 0) currentCooldownTimer -= Time.deltaTime;

        if (playerController != null && playerController.IsDashing) {
            if (IsCharging) CancelCharge();
            return;
        }

        if (Input.GetKeyDown(KeyCode.Space) && currentCooldownTimer <= 0f) {
            if (playerCombat != null && playerCombat.IsAttacking) playerCombat.CancelAttack();
            if (playerDefense != null && playerDefense.IsBlocking) playerDefense.StopBlocking(true);

            StartCharging();
        }

        if (Input.GetKey(KeyCode.Space) && IsCharging) {
            currentChargeTimer += Time.deltaTime;

            // --- Calculate the overall charge percentage (0.0 to 1.0) ---
            float chargePercent = Mathf.Clamp01(currentChargeTimer / chargeTimeRequired);

            // --- NEW: Smoothly fade the fire audio volume up! ---
            if (chargeAudioSource != null) {
                chargeAudioSource.volume = chargePercent * maxChargeVolume;
            }

            if (fullyOnFireVFX != null && !hasPlayedMaxChargePing) {
                fullyOnFireVFX.transform.localScale = originalFireScale * chargePercent;
            }

            if (currentChargeTimer >= chargeTimeRequired && !hasPlayedMaxChargePing) {
                hasPlayedMaxChargePing = true;

                if (maxChargeRingPrefab != null && talismanMesh != null) {
                    Instantiate(maxChargeRingPrefab, talismanMesh.transform.position, talismanMesh.transform.rotation);
                }

                if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX3D("Magic_Max_Ping", transform.position);
            }
        }

        if (Input.GetKeyUp(KeyCode.Space)) {
            if (IsCharging) FireShot();
        }
    }

    void StartCharging() {
        if (IsCharging) return;
        IsCharging = true;
        currentChargeTimer = 0f;
        hasPlayedMaxChargePing = false;

        if (fullyOnFireVFX != null) {
            fullyOnFireVFX.transform.localScale = Vector3.zero;
            fullyOnFireVFX.Play(true);
        }

        // --- NEW: Start the audio source at 0 volume ---
        if (chargeAudioSource != null) {
            chargeAudioSource.volume = 0f;
            if (!chargeAudioSource.isPlaying) {
                chargeAudioSource.Play();
            }
        }

        if (anim != null) anim.SetBool("IsChargingRanged", true);
        if (CameraManager.Instance != null) CameraManager.Instance.SwitchToAim();
    }

    public void CancelCharge() {
        if (!IsCharging) return;
        IsCharging = false;
        currentChargeTimer = 0f;

        HideTalisman();

        if (anim != null) {
            anim.SetBool("IsChargingRanged", false);
            anim.ResetTrigger("FireRanged");
            anim.CrossFade("New State", 0.1f, 1);
        }

        if (CameraManager.Instance != null) CameraManager.Instance.SwitchToDefault();
    }

    void FireShot() {
        if (anim != null) {
            anim.SetBool("IsChargingRanged", false);
            anim.SetTrigger("FireRanged");
        }

        if (currentChargeTimer >= chargeTimeRequired) {
            SpawnProjectile(chargedProjectileTag);
            currentCooldownTimer = chargedCooldown;
            if (impulseSource != null) impulseSource.GenerateImpulse(1.5f);

            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX3D("Magic_Shoot_Heavy", transform.position);
        } else {
            SpawnProjectile(instantProjectileTag);
            currentCooldownTimer = normalCooldown;
            if (impulseSource != null) impulseSource.GenerateImpulse(0.3f);

            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX3D("Magic_Shoot_Normal", transform.position);
        }

        IsCharging = false;
        currentChargeTimer = 0f;

        // Ensure the talisman and fire sound are hidden/stopped upon firing
        HideTalisman();

        if (CameraManager.Instance != null) CameraManager.Instance.SwitchToDefault();
    }

    void SpawnProjectile(string poolTag) {
        if (string.IsNullOrEmpty(poolTag) || firePoint == null || mainCam == null) return;

        Ray ray = mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        Vector3 targetPoint;

        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, aimMask)) {
            targetPoint = hit.point;
        } else {
            targetPoint = ray.GetPoint(100f);
        }

        Vector3 aimDir = (targetPoint - firePoint.position).normalized;
        Quaternion bulletRot = Quaternion.LookRotation(aimDir);

        GameObject proj = ProjectilePooler.Instance.GetProjectile(poolTag, firePoint.position, bulletRot);

        Projectile projScript = proj.GetComponent<Projectile>();
        if (projScript != null) {
            projScript.SetShooter(transform);
        }
    }


    // ==========================================
    // --- ANIMATOR EVENT METHODS ---
    // ==========================================

    public void ShowTalisman() {
        if (talismanMesh != null) {
            Renderer[] renderers = talismanMesh.GetComponentsInChildren<Renderer>();
            foreach (Renderer r in renderers) {
                r.enabled = true;
            }
        }
    }

    public void HideTalisman() {
        if (talismanMesh != null) {
            Renderer[] renderers = talismanMesh.GetComponentsInChildren<Renderer>();
            foreach (Renderer r in renderers) {
                r.enabled = false;
            }
        }

        // Instantly kill the fire VFX
        if (fullyOnFireVFX != null) {
            fullyOnFireVFX.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            fullyOnFireVFX.transform.localScale = Vector3.zero;
        }

        // --- NEW: Instantly kill the charging audio! ---
        if (chargeAudioSource != null && chargeAudioSource.isPlaying) {
            chargeAudioSource.Stop();
        }
    }
}