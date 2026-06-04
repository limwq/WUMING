using UnityEngine;
using System.Collections;

public class Ability_GhostArea : BossAbility {

    [Header("Ghost Area Burst Settings")]
    [Tooltip("How much damage the explosion deals")]
    [SerializeField] private float explosionDamage = 40f;
    [Tooltip("How far the explosion reaches from the boss")]
    [SerializeField] private float dangerRadius = 6f;
    [Tooltip("How much physical force throws the player backward")]
    [SerializeField] private float knockbackForce = 15f;
    [Tooltip("How long the boss gathers energy before it blows up")]
    [SerializeField] private float buildupTime = 2.0f;

    [Header("VFX Tags")]
    [Tooltip("The dark spirits gathering into the boss")]
    [SerializeField] private string gatherVfxTag = "GhostGather";
    [Tooltip("The violent explosion from the boss's center")]
    [SerializeField] private string burstVfxTag = "GhostAreaBurst";

    // --- NEW: Exposed Audio Tags ---
    [Header("Audio Tags")]
    [SerializeField] private string suckAudioTag = "Boss_Ghost_Suck";
    [SerializeField] private string burstAudioTag = "Boss_Ghost_Burst";

    private Transform playerTransform;

    private void Start() {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) playerTransform = p.transform;
    }

    protected override IEnumerator CastLogic() {
        // 1. THE WINDUP ANIMATION
        if (anim != null) {
            anim.SetTrigger("CastArea");
            Debug.Log("<color=magenta>[Boss] Gathering dark energy!</color>");
        }

        Transform bossTransform = GetComponentInParent<BossBrain>().transform;

        // Spawn it 1 meter in front of the boss, 1 meter off the ground
        Vector3 bossCenter = bossTransform.position + (bossTransform.forward * 1f) + (Vector3.up * 1f);

        // 2. THE GATHERING VFX & AUDIO
        if (VFXManager.Instance != null) {
            VFXManager.Instance.SpawnVFX(gatherVfxTag, bossCenter, Quaternion.identity);
        }

        // --- THE UPGRADE: Uses our tracked audio helper so it can be interrupted! ---
        if (!string.IsNullOrEmpty(suckAudioTag)) {
            PlayAbilitySound(suckAudioTag, bossCenter); // Play it at the ball's location!
        }

        // 3. THE SUSPENSE (Wait for the energy ball to hit critical mass)
        yield return new WaitForSeconds(buildupTime - 1f);

        // 4. THE BURST (Explosion VFX, Audio, and Camera Shake)
        if (VFXManager.Instance != null) {
            VFXManager.Instance.SpawnVFX(burstVfxTag, bossCenter, Quaternion.identity);
        }

        // --- THE UPGRADE: Uses our tracked audio helper! ---
        if (!string.IsNullOrEmpty(burstAudioTag)) {
            PlayAbilitySound(burstAudioTag, bossCenter);
        }

        if (JuiceManager.Instance != null) {
            JuiceManager.Instance.ShakeCamera(2.0f); // Big shake for the explosion!
        }

        // 5. THE DAMAGE & KNOCKBACK CALCULATION
        if (playerTransform != null) {
            float distanceFromBoss = Vector3.Distance(bossCenter, playerTransform.position);

            if (distanceFromBoss <= dangerRadius) {
                Debug.Log($"<color=red>[Boss] Player caught in the blast! Dealt {explosionDamage} damage.</color>");

                Vector3 knockbackDirection = (playerTransform.position - bossCenter).normalized;
                knockbackDirection.y = 0.5f;

                // --- APPLY DAMAGE AND KNOCKBACK ---
                PlayerController pc = playerTransform.GetComponent<PlayerController>();
                if (pc != null) {
                    pc.ApplyKnockback(knockbackDirection * knockbackForce);
                }

                PlayerHealth ph = playerTransform.GetComponent<PlayerHealth>();
                if (ph != null) {
                    ph.TakeDamage(explosionDamage);
                }
            } else {
                Debug.Log("<color=green>[Boss] Player safely out of range of the burst!</color>");
            }
        }

        // 6. RECOVERY
        float recoveryTime = Mathf.Max(0f, castDuration - buildupTime);
        yield return new WaitForSeconds(recoveryTime);
    }

    private void OnDrawGizmosSelected() {
        Gizmos.color = new Color(1, 0, 0, 0.3f);

        Transform bossTransform = transform.root; // Quick fallback for the editor
        BossBrain brain = GetComponentInParent<BossBrain>();
        if (brain != null) bossTransform = brain.transform;

        // --- THE FIX: Changed 2f to 1f so the red debug sphere matches the actual explosion! ---
        Vector3 bossCenter = bossTransform.position + (bossTransform.forward * 1f) + (Vector3.up * 1f);
        Gizmos.DrawSphere(bossCenter, dangerRadius);
    }
}