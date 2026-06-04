using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerHealth : MonoBehaviour {
    [Header("Stats")]
    [SerializeField] private float maxHealth = 100f;
    private float currentHealth;

    [Header("UI")]
    public Slider healthSlider;
    private PlayerController playerController;
    private PlayerDefense playerDefense;
    private Animator anim;

    public bool IsDead { get; private set; } = false;

    void Awake() {
        playerController = GetComponent<PlayerController>();
        playerDefense = GetComponent<PlayerDefense>();
        anim = GetComponentInChildren<Animator>();
        currentHealth = maxHealth;

        if (healthSlider != null) {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }
    }

    // --- NEW: Added impactPoint with a default fallback ---
    public void TakeDamage(float damageAmount, Vector3 impactPoint = default) {
        if (IsDead) return;
        if (playerController != null && playerController.IsDashing) return;

        // 1. CHECK PERFECT BLOCK (Melee)
        if (playerDefense != null && playerDefense.IsPerfectBlocking()) {
            // --- NEW: Pass the impact point to the Defense script! ---
            playerDefense.TriggerPerfectBlock(true, impactPoint);
            Debug.Log(impactPoint != default ? $"Perfect Block at {impactPoint}!" : "Perfect Block!");
            return;
        }

        // 2. CHECK NORMAL BLOCK
        if (playerDefense != null && playerDefense.IsBlocking) {
            Debug.Log("<color=yellow>Blocked.</color>");
            if (JuiceManager.Instance != null) {
                JuiceManager.Instance.ShakeCamera(0.5f); // Slightly weaker shake for normal block
            }
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX3D("Player_Block", transform.position);
            return;
        }

        // 3. TAKE DAMAGE
        currentHealth -= damageAmount;
        if (healthSlider != null) healthSlider.value = currentHealth;

        if (JuiceManager.Instance != null) {
            JuiceManager.Instance.ShakeCamera(1f);
        }

        // --- THE REFINED KNOCKBACK HOOK ---
        Vector3 knockbackDir;
        if (impactPoint != default) {
            // Push the player directly AWAY from the exact point the weapon hit them
            knockbackDir = (transform.position - impactPoint).normalized;
        } else {
            // Fallback: If no point was provided, just push them straight backward
            knockbackDir = -transform.forward;
        }

        // Add a tiny bit of upward lift so they don't drag on the floor collider
        knockbackDir.y = 0.2f;
        knockbackDir = knockbackDir.normalized;

        // Trigger the knockback! (Multiply by a force number like 5f or 8f so it actually pushes them)
        if (playerController != null) {
            playerController.ApplyKnockback(knockbackDir * 20f);
        }

        DamageFlash flash = GetComponent<DamageFlash>();
        if (flash != null) flash.TriggerFlash();

        // --- NEW: SPAWN PLAYER BLOOD! ---
        if (VFXManager.Instance != null) {
            Vector3 bloodPos = impactPoint != default ? impactPoint : transform.position + (Vector3.up * 1f);
            // Splatters blood in the opposite direction the player is facing
            VFXManager.Instance.SpawnBlood(bloodPos, -transform.forward);
        }

        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX3D("Player_Hurt", transform.position);

        if (currentHealth <= 0) {
            Die();
        } else {
            if (anim != null) anim.SetTrigger("GetHit");
            
        }
    }

    void Die() {
        if (IsDead) return;

        Debug.Log("Player Died!");

        // --- THE FIX: Instantly lock all controls so they can't move as a corpse! ---
        if (playerController != null) {
            playerController.SetPlayerControl(false);
        }

        anim.SetTrigger("Die");

        StartCoroutine(GameOverSequence());
    }

    private IEnumerator GameOverSequence() {
        // Wait for 3 seconds so the player watches their body hit the floor
        yield return new WaitForSeconds(3.0f);

        // Call your AAA Scene Manager!
        if (GameSceneManager.Instance != null) {
            GameSceneManager.Instance.FadeScene("GameOver");
        } else {
            Debug.LogWarning("GameSceneManager is missing!");
        }
    }

    // ==========================================
    // --- NEW: HEALING LOGIC ---
    // ==========================================
    public void Heal(float healAmount) {
        // Prevent healing if the player is already dead
        if (currentHealth <= 0) return;

        currentHealth += healAmount;

        // Clamp the health so it never goes above the max!
        if (currentHealth > maxHealth) {
            currentHealth = maxHealth;
        }

        // Update the UI Slider
        if (healthSlider != null) {
            healthSlider.value = currentHealth;
        }

        // Optional AAA Polish: Add a heal sound or particle effect here later!
        // if (VFXManager.Instance != null) VFXManager.Instance.SpawnVFX("HealSparkles", transform.position);

        Debug.Log($"<color=green>Player Healed! Current HP: {currentHealth}/{maxHealth}</color>");
    }
}