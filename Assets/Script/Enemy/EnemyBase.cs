using UnityEngine;
using UnityEngine.UI;

public class EnemyBase : MonoBehaviour {
    [Header("Base Stats")]
    [SerializeField] protected float maxHealth = 100f;
    public float currentHealth;

    [Header("UI (Optional)")]
    [SerializeField] private Slider healthSlider;

    [Header("VFX Settings")]
    [SerializeField] protected Transform hurtPoint;
    [SerializeField] protected Transform spiritSpawnPoint;
    [SerializeField] protected GameObject auraVisual;

    [Header("Audio Settings")]
    [SerializeField] protected string hurtAudioTag = "Enemy_Hurt";
    [SerializeField] protected string deathAudioTag = "Enemy_Die";
    [SerializeField] protected string deathHowlAudioTag = "Enemy_DieHowl";

    // --- NEW: Cleanup Settings ---
    [Header("Cleanup Settings")]
    [Tooltip("How many seconds the corpse stays on the ground before disappearing")]
    [SerializeField] protected float destroyDelay = 5f;

    protected Animator anim;
    protected bool isDead = false;
    protected bool isAggroed = false;

    protected virtual void Start() {
        anim = GetComponentInChildren<Animator>();
        currentHealth = maxHealth;

        if (healthSlider != null) {
            healthSlider.value = currentHealth;
        }

        if (VFXManager.Instance != null) {
            VFXManager.Instance.SpawnVFX("SpawnSmoke", transform.position - (Vector3.up * 0.5f), Quaternion.identity);
        }
    }

    public void SetAggro(bool hasSpottedPlayer) {
        if ((isDead && hasSpottedPlayer) || isAggroed == hasSpottedPlayer) return;

        isAggroed = hasSpottedPlayer;

        if (isAggroed) {
            if (AudioManager.Instance != null) AudioManager.Instance.AddAggro();
        } else {
            if (AudioManager.Instance != null) AudioManager.Instance.RemoveAggro();
        }
    }

    public virtual void TakeDamage(float damage, Vector3 impactPoint = default) {
        if (isDead) return;

        currentHealth -= damage;
        UpdateHealthUI();

        DamageFlash flash = GetComponent<DamageFlash>();
        if (flash != null) flash.TriggerFlash();

        Vector3 woundPos = impactPoint != default ? impactPoint : (hurtPoint != null ? hurtPoint.position : transform.position + (Vector3.up * 1f));

        if (VFXManager.Instance != null) {
            VFXManager.Instance.SpawnBlood(woundPos, transform.forward);
        }

        if (currentHealth <= 0) {
            Die();
        } else {
            if (AudioManager.Instance != null && !string.IsNullOrEmpty(hurtAudioTag)) {
                AudioManager.Instance.PlaySFX3D(hurtAudioTag, woundPos);
            }
        }
    }

    protected virtual void UpdateHealthUI() {
        if (healthSlider != null) {
            healthSlider.value = currentHealth / maxHealth;
        }
    }

    protected virtual void Die() {
        if (isAggroed) {
            SetAggro(false);
        }

        isDead = true;

        StopAllCoroutines();

        if (auraVisual != null) auraVisual.SetActive(false);

        if (AudioManager.Instance != null && !string.IsNullOrEmpty(deathAudioTag)) {
            AudioManager.Instance.PlaySFX3D(deathAudioTag, transform.position);
        }

        AudioSource[] attachedAudio = GetComponentsInChildren<AudioSource>();
        foreach (AudioSource source in attachedAudio) {
            if (source != null && source.isPlaying) {
                source.Stop();
            }
        }

        if (anim != null) {
            anim.ResetTrigger("GetHit");
            anim.ResetTrigger("Attack");
            anim.SetBool("IsWalking", false);
            anim.SetTrigger("Die");
        }

        if (healthSlider != null) healthSlider.gameObject.SetActive(false);

        Vector3 spiritPos = spiritSpawnPoint != null ? spiritSpawnPoint.position : transform.position + (Vector3.up * 1.5f);
        if (VFXManager.Instance != null) {
            VFXManager.Instance.SpawnDeathSpirit(spiritPos);
        }

        if (AudioManager.Instance != null && !string.IsNullOrEmpty(deathHowlAudioTag)) {
            AudioManager.Instance.PlaySFX3D(deathHowlAudioTag, transform.position);
        }

        int deadLayer = LayerMask.NameToLayer("DeadEnemy");
        Transform[] allChildren = GetComponentsInChildren<Transform>(true);

        foreach (Transform child in allChildren) {
            if (deadLayer != -1) {
                child.gameObject.layer = deadLayer;
            }
            child.gameObject.tag = "Untagged";
        }

        // ==========================================
        // --- THE FIX: MEMORY CLEANUP ---
        // ==========================================
        // This tells Unity to completely delete the object and free up the RAM after X seconds!
        Destroy(gameObject, destroyDelay);
    }

    protected virtual void OnDisable() {
        if (isAggroed) {
            SetAggro(false);
        }
    }
}