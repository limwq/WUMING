using UnityEngine;
using System.Collections;
using UnityEngine.AI;

public class Ability_Thunder : BossAbility {
    [Header("Thunder Ultimate Settings")]
    [SerializeField] private float warningDuration = 5f;
    [SerializeField] private int instantKillDamage = 9999;

    [Header("Flight & Slam Settings")]
    [Tooltip("How high above the ground should the thundercloud spawn?")]
    [SerializeField] private float cloudSpawnHeight = 20f;
    [SerializeField] private float floatHeight = 10f;
    [SerializeField] private float slamDuration = 0.1f;
    [SerializeField] private Transform arenaCenter;

    [Header("Line of Sight & Safe Zones")]
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private GameObject playerSafeAuraVFX;

    // --- NEW: UI Warning Settings ---
    [Header("UI Warning Settings")]
    [Tooltip("Drag the CanvasGroup attached to your Warning Text here")]
    [SerializeField] private CanvasGroup warningTextCanvasGroup;
    [Tooltip("How fast the text fades in and out (in seconds)")]
    [SerializeField] private float textFadeDuration = 0.5f;

    [Header("Apocalyptic VFX Tags")]
    [SerializeField] private string chargeVfxTag = "UltimateCharge";
    [SerializeField] private string nukeVfxTag = "UltimateNuke";
    [Tooltip("The tag for the dynamic thundercloud ceiling")]
    [SerializeField] private string thunderCloudVfxTag = "ThunderCloud";

    [Header("Random Lightning Storm Settings")]
    [SerializeField] private string lightningVfxTag = "SummonLightning";
    [SerializeField] private float stormRadius = 25f;
    [SerializeField] private float lightningInterval = 0.3f;

    [Header("Audio Tags")]
    [Tooltip("Sound when the boss begins floating into the air")]
    [SerializeField] private string ascendAudioTag = "Ult_Ascend";
    [Tooltip("Sound for the random lightning bolts during the storm")]
    [SerializeField] private string strikeAudioTag = "Ult_ThunderStrike";
    [Tooltip("Deafening 2D sound played directly in the player's ears")]
    [SerializeField] private string nukeUIAudioTag = "Ult_Nuke_Explosion";
    [Tooltip("Massive 3D explosion sound at the boss's location")]
    [SerializeField] private string nuke3DAudioTag = "Ult_Thunder_Explosion";

    private bool isPlayerSafe = false;
    private Vector3 groundPos;
    private Vector3 airPos;

    private GameObject activeChargeVFX;
    private GameObject activeCloudVFX;
    private Coroutine stormCoroutine;
    private Coroutine warningTextCoroutine; // --- NEW: Tracks the fading text

    protected override IEnumerator CastLogic() {
        Debug.Log("<color=red>[THUNDER] APOCALYPSE INITIATED!</color>");

        NavMeshAgent agent = transform.root.GetComponent<NavMeshAgent>();

        // --- PHASE 0: POSITIONING (Arena Center) ---
        if (arenaCenter != null) {
            yield return StartCoroutine(ExecuteBlink(arenaCenter.position));
            Vector3 dir = (playerTarget.position - transform.root.position).normalized;
            transform.root.forward = new Vector3(dir.x, 0, dir.z);
        }

        groundPos = transform.root.position;
        airPos = new Vector3(groundPos.x, groundPos.y + floatHeight, groundPos.z);

        if (agent != null) agent.enabled = false;

        // --- PHASE 1: WINDUP (SPAWN CLOUD & STORM) ---
        if (anim != null) anim.SetTrigger("ChargeUltimate");

        // 1. Spawn the Dynamic Thundercloud Ceiling 
        if (VFXManager.Instance != null && !string.IsNullOrEmpty(thunderCloudVfxTag)) {
            Vector3 cloudPos = new Vector3(groundPos.x, groundPos.y + cloudSpawnHeight, groundPos.z);
            activeCloudVFX = VFXManager.Instance.SpawnVFX(thunderCloudVfxTag, cloudPos, Quaternion.identity, false);
        }

        if (!string.IsNullOrEmpty(ascendAudioTag)) {
            PlayAbilitySound(ascendAudioTag, transform.position, false);
        }

        // 2. Black Hole VFX
        if (VFXManager.Instance != null) {
            Vector3 chargePos = transform.root.position + (Vector3.up * 2f);
            activeChargeVFX = VFXManager.Instance.SpawnVFX(chargeVfxTag, chargePos, Quaternion.identity);
            if (activeChargeVFX != null) activeChargeVFX.transform.SetParent(transform.root);
        }

        // --- THE UPGRADE: Start the UI Warning Fade! ---
        if (warningTextCoroutine != null) StopCoroutine(warningTextCoroutine);
        warningTextCoroutine = StartCoroutine(ShowWarningTextRoutine());

        // 3. Start Storm (originating from the cloud height)
        if (stormCoroutine != null) StopCoroutine(stormCoroutine);
        stormCoroutine = StartCoroutine(LightningStormRoutine(new Vector3(groundPos.x, groundPos.y + cloudSpawnHeight, groundPos.z)));

        float timer = 0f;
        while (timer < warningDuration) {
            timer += Time.deltaTime;
            float floatProgress = timer / warningDuration;

            // Boss smoothly floats up towards the clouds
            transform.root.position = Vector3.Lerp(groundPos, airPos, floatProgress);

            // Hide-and-seek LoS check
            isPlayerSafe = CheckLineOfSight();
            if (playerSafeAuraVFX != null) playerSafeAuraVFX.SetActive(isPlayerSafe);

            yield return null;
        }

        transform.root.position = airPos;

        // --- PHASE 2: THE SLAM ---
        if (anim != null) anim.SetTrigger("CastUltimate");

        if (stormCoroutine != null) StopCoroutine(stormCoroutine);
        if (activeChargeVFX != null) {
            activeChargeVFX.SetActive(false);
            activeChargeVFX.transform.SetParent(null);
        }

        float slamTimer = 0f;
        while (slamTimer < slamDuration) {
            slamTimer += Time.deltaTime;
            float slamProgress = slamTimer / slamDuration;
            float gravityCurve = slamProgress * slamProgress;
            transform.root.position = Vector3.Lerp(airPos, groundPos, gravityCurve);
            yield return null;
        }

        transform.root.position = groundPos;
        if (agent != null) agent.enabled = true;

        // --- PHASE 3: THE NUKE (Obliteration) ---
        if (playerSafeAuraVFX != null) playerSafeAuraVFX.SetActive(false);

        // Explosion VFX
        if (VFXManager.Instance != null) {
            VFXManager.Instance.SpawnVFX(nukeVfxTag, groundPos, Quaternion.identity);
        }

        // Dual Explosion Audio
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(nukeUIAudioTag)) {
            AudioManager.Instance.PlayUI(nukeUIAudioTag);
        }

        if (!string.IsNullOrEmpty(nuke3DAudioTag)) {
            PlayAbilitySound(nuke3DAudioTag, transform.position);
        }

        ScreenFlash.Flash(Color.white, 1.5f);
        if (JuiceManager.Instance != null) {
            JuiceManager.Instance.ShakeCamera(3.0f);
        }

        if (!isPlayerSafe) {
            Debug.Log("<color=red>[THUNDER] WIPE MECHANIC DETONATED! Player obliterated.</color>");
            PlayerHealth playerHealth = playerTarget.GetComponent<PlayerHealth>();
            if (playerHealth != null) playerHealth.TakeDamage(instantKillDamage);
        } else {
            Debug.Log("<color=green>[THUNDER] Player survived by hiding!</color>");
        }

        // --- PHASE 4: CLEANUP & FADE OUT CLOUD ---
        if (activeCloudVFX != null) {
            ParticleSystem[] cloudSystems = activeCloudVFX.GetComponentsInChildren<ParticleSystem>();
            foreach (var ps in cloudSystems) {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }

            StartCoroutine(DespawnCloudAfterDelay(activeCloudVFX, 10f));
            activeCloudVFX = null;
        }

        yield return new WaitForSeconds(castDuration - warningDuration - slamDuration);
    }

    // --- NEW: The UI Fading Logic ---
    private IEnumerator ShowWarningTextRoutine() {
        if (warningTextCanvasGroup == null) yield break;

        // 1. Fade In
        float fadeTimer = 0f;
        while (fadeTimer < textFadeDuration) {
            fadeTimer += Time.deltaTime;
            warningTextCanvasGroup.alpha = Mathf.Lerp(0f, 1f, fadeTimer / textFadeDuration);
            yield return null;
        }
        warningTextCanvasGroup.alpha = 1f;

        // 2. Wait while the boss is charging (Calculate how long to stay fully visible)
        float waitTime = warningDuration - (textFadeDuration * 2);
        if (waitTime > 0) {
            yield return new WaitForSeconds(waitTime);
        }

        // 3. Fade Out exactly as the nuke hits
        fadeTimer = 0f;
        while (fadeTimer < textFadeDuration) {
            fadeTimer += Time.deltaTime;
            warningTextCanvasGroup.alpha = Mathf.Lerp(1f, 0f, fadeTimer / textFadeDuration);
            yield return null;
        }
        warningTextCanvasGroup.alpha = 0f;
    }

    private IEnumerator LightningStormRoutine(Vector3 stormOriginCenter) {
        while (true) {
            Vector2 randomCircle = Random.insideUnitCircle * stormRadius;
            Vector3 strikePoint = stormOriginCenter + new Vector3(randomCircle.x, 0, randomCircle.y);
            Vector3 startPos = strikePoint;

            if (VFXManager.Instance != null) {
                Quaternion downwardRotation = Quaternion.LookRotation(Vector3.up);
                VFXManager.Instance.SpawnVFX(lightningVfxTag, startPos, downwardRotation);
            }

            if (!string.IsNullOrEmpty(strikeAudioTag)) {
                PlayAbilitySound(strikeAudioTag, strikePoint, false);
            }

            if (JuiceManager.Instance != null) JuiceManager.Instance.ShakeCamera(0.2f);

            if (playerTarget != null && Vector3.Distance(new Vector3(strikePoint.x, 0, strikePoint.z), playerTarget.position) < 2.0f) {
                PlayerHealth ph = playerTarget.GetComponent<PlayerHealth>();
                if (ph != null) ph.TakeDamage(15f);
            }

            yield return new WaitForSeconds(lightningInterval);
        }
    }

    private IEnumerator DespawnCloudAfterDelay(GameObject cloudObj, float delay) {
        yield return new WaitForSeconds(delay);
        if (cloudObj != null) {
            if (VFXManager.Instance != null) {
                VFXManager.Instance.ManualReturnToPool(thunderCloudVfxTag, cloudObj);
            } else {
                cloudObj.SetActive(false);
            }
        }
    }

    protected override void CleanUpOnInterrupt() {
        if (playerSafeAuraVFX != null) playerSafeAuraVFX.SetActive(false);
        if (stormCoroutine != null) StopCoroutine(stormCoroutine);

        // --- NEW: Clean up the UI if the boss is interrupted! ---
        if (warningTextCoroutine != null) StopCoroutine(warningTextCoroutine);
        if (warningTextCanvasGroup != null) warningTextCanvasGroup.alpha = 0f;

        if (activeChargeVFX != null) {
            activeChargeVFX.SetActive(false);
            activeChargeVFX.transform.SetParent(null);
        }

        if (activeCloudVFX != null) {
            activeCloudVFX.SetActive(false);
            activeCloudVFX = null;
        }

        NavMeshAgent agent = transform.root.GetComponent<NavMeshAgent>();
        if (agent != null && !agent.enabled) {
            transform.root.position = groundPos;
            agent.enabled = true;
        }
    }

    private bool CheckLineOfSight() {
        if (playerTarget == null) return false;
        Vector3 origin = transform.root.position + Vector3.up * 1.5f;
        Vector3 targetPos = playerTarget.position + Vector3.up * 1.5f;
        Vector3 direction = targetPos - origin;
        float distanceToPlayer = direction.magnitude;
        int combinedMask = obstacleLayer | playerLayer;
        if (Physics.Raycast(origin, direction.normalized, out RaycastHit hit, distanceToPlayer + 1f, combinedMask)) {
            if ((obstacleLayer.value & (1 << hit.collider.gameObject.layer)) > 0) return true;
        }
        return false;
    }
}