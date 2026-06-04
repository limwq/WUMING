using UnityEngine;
using System.Collections;

public class Ability_Projectile : BossAbility {

    [Header("Barrage Settings (Pool)")]
    [Tooltip("Must match the exact tag in the ProjectilePooler")]
    public string talismanPoolTag = "BossTalisman";
    public Transform firePoint;
    public int shotCount = 10;
    public float timeBetweenShots = 0.15f;

    [Header("Visuals")]
    [SerializeField] private ParticleSystem[] handChargeVFX;
    [Tooltip("How fast the VFX scales up and down")]
    [SerializeField] private float vfxScaleDuration = 0.5f;

    // --- NEW: Exposed Audio Tag ---
    [Header("Audio")]
    [Tooltip("A long, continuous sound that plays during the entire barrage")]
    [SerializeField] private string barrageAudioTag = "Boss_Soul";

    private Vector3[] originalVFXScales;
    private Coroutine scaleCoroutine;

    public override void Awake() {
        if (handChargeVFX != null) {
            originalVFXScales = new Vector3[handChargeVFX.Length];
            for (int i = 0; i < handChargeVFX.Length; i++) {
                if (handChargeVFX[i] != null) {
                    originalVFXScales[i] = handChargeVFX[i].transform.localScale;
                    handChargeVFX[i].transform.localScale = Vector3.zero;
                }
            }
        }
        base.Awake();
    }

    protected override IEnumerator CastLogic() {
        if (playerTarget == null || string.IsNullOrEmpty(talismanPoolTag) || firePoint == null) yield break;

        if (anim != null) anim.SetTrigger("CastProjectile");

        if (playerTarget != null) {
            Vector3 LookDir = (playerTarget.position - transform.root.position).normalized;
            LookDir.y = 0;

            if (LookDir != Vector3.zero) {
                Quaternion targetRotation = Quaternion.LookRotation(LookDir);
                transform.root.rotation = Quaternion.Slerp(transform.root.rotation, targetRotation, Time.deltaTime * 15f);
            }
        }

        // 1. THE WINDUP: Smoothly scale up and ignite the hands!
        if (scaleCoroutine != null) StopCoroutine(scaleCoroutine);
        scaleCoroutine = StartCoroutine(ScaleHandVFX(true));

        Transform playerAimPoint = playerTarget.Find("AimPoint");

        // --- THE AUDIO FIX: Play it once, and track it! ---
        if (!string.IsNullOrEmpty(barrageAudioTag)) {
            PlayAbilitySound(barrageAudioTag, firePoint.position);
        }

        yield return new WaitForSeconds(castDuration * 0.2f);

        // 2. THE BARRAGE
        for (int i = 0; i < shotCount; i++) {
            if (playerTarget == null) break;

            Vector3 targetChest = playerAimPoint != null ? playerAimPoint.position : playerTarget.position + (Vector3.up * 1f);
            Vector3 aimDir = (targetChest - firePoint.position).normalized;

            aimDir.x += Random.Range(-0.01f, 0.01f);
            aimDir.y += Random.Range(-0.01f, 0.01f);

            Quaternion bulletRot = Quaternion.LookRotation(aimDir);

            GameObject proj = ProjectilePooler.Instance.GetProjectile(talismanPoolTag, firePoint.position, bulletRot);

            if (proj != null) {
                Projectile projScript = proj.GetComponent<Projectile>();
                if (projScript != null) {
                    projScript.SetShooter(transform.root);
                }
            }

            if (JuiceManager.Instance != null) {
                JuiceManager.Instance.ShakeCamera(0.1f);
            }

            float timer = 0f;
            while (timer < timeBetweenShots) {
                if (playerTarget != null) {
                    Vector3 bodyLookDir = (playerTarget.position - transform.root.position).normalized;
                    bodyLookDir.y = 0;

                    if (bodyLookDir != Vector3.zero) {
                        Quaternion targetRotation = Quaternion.LookRotation(bodyLookDir);
                        transform.root.rotation = Quaternion.Slerp(transform.root.rotation, targetRotation, Time.deltaTime * 15f);
                    }
                }

                timer += Time.deltaTime;
                yield return null;
            }
        }

        // 3. CLEANUP: Smoothly shrink and turn off the hand fire
        if (scaleCoroutine != null) StopCoroutine(scaleCoroutine);
        scaleCoroutine = StartCoroutine(ScaleHandVFX(false));

        // --- THE AUDIO FIX: Force the long audio to stop if the barrage finishes early! ---
        foreach (AudioSource source in activeSounds) {
            if (source != null && source.isPlaying) {
                source.Stop();
                source.gameObject.SetActive(false); // Return to pool
            }
        }
        activeSounds.Clear();

        // 4. RECOVERY
        yield return new WaitForSeconds(castDuration * 0.2f);
    }

    private IEnumerator ScaleHandVFX(bool scaleUp) {
        if (scaleUp && handChargeVFX != null) {
            foreach (var vfx in handChargeVFX) {
                if (vfx != null && !vfx.isPlaying) vfx.Play(true);
            }
        }

        float timer = 0f;

        while (timer < vfxScaleDuration) {
            timer += Time.deltaTime;
            float t = timer / vfxScaleDuration;

            for (int i = 0; i < handChargeVFX.Length; i++) {
                if (handChargeVFX[i] != null) {
                    Vector3 targetScale = scaleUp ? originalVFXScales[i] : Vector3.zero;
                    handChargeVFX[i].transform.localScale = Vector3.Slerp(handChargeVFX[i].transform.localScale, targetScale, t);
                }
            }
            yield return null;
        }

        for (int i = 0; i < handChargeVFX.Length; i++) {
            if (handChargeVFX[i] != null) {
                handChargeVFX[i].transform.localScale = scaleUp ? originalVFXScales[i] : Vector3.zero;

                if (!scaleUp) {
                    handChargeVFX[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
            }
        }
    }

    public override void InterruptAbility() {
        if (scaleCoroutine != null) StopCoroutine(scaleCoroutine);

        if (handChargeVFX != null) {
            for (int i = 0; i < handChargeVFX.Length; i++) {
                if (handChargeVFX[i] != null) {
                    handChargeVFX[i].transform.localScale = Vector3.zero;
                    handChargeVFX[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
            }
        }

        Debug.Log("<color=yellow>[Boss Projectile] Ability Interrupted! Hand VFX stopped and hidden.</color>");

        // This will automatically kill the audio via the tracked activeSounds list!
        base.InterruptAbility();
    }
}