using UnityEngine;
using System.Collections.Generic;

public class DamageHitbox : MonoBehaviour {
    [SerializeField] private int damageAmount = 10;
    [SerializeField] private string targetTag = "Player"; // Change to "Enemy" on the Player's sword

    [Header("Visuals")]
    [SerializeField] private MeshRenderer swordMesh; // Drag the sword's MeshRenderer here
    [SerializeField] private TrailRenderer weaponTrail;
    [SerializeField] private ParticleSystem bossWeaponBleed;

    private List<GameObject> hitList = new List<GameObject>();
    private Collider hitCollider;

    void Awake() {
        hitCollider = GetComponent<Collider>();

        // If you didn't drag it in the inspector, try to find it automatically
        if (swordMesh == null) swordMesh = GetComponent<MeshRenderer>();

        // Ensure both the visuals and the hitbox start OFF
        DisableHitbox();
    }

    // --- Called by the Animator Event ---
    public void EnableHitbox() {
        hitList.Clear();

        if (hitCollider != null) hitCollider.enabled = true;   // Turn on damage
        if (swordMesh != null) swordMesh.enabled = true;       // Make it visible
        if (weaponTrail != null) weaponTrail.emitting = true;
        if (bossWeaponBleed != null) bossWeaponBleed.Play(true);
    }

    // --- Called by the Animator Event ---
    public void DisableHitbox() {
        if (hitCollider != null) hitCollider.enabled = false;  // Turn off damage
        if (swordMesh != null) swordMesh.enabled = false;      // Make it invisible
        if (weaponTrail != null) weaponTrail.emitting = false;
        if (bossWeaponBleed != null) bossWeaponBleed.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    public void ShowWeaponMeshOnly() {

        if (hitCollider != null) hitCollider.enabled = false;   // Turn off damage
        if (swordMesh != null) swordMesh.enabled = true;       // Make it visible
        if (weaponTrail != null) weaponTrail.emitting = true;
        if (bossWeaponBleed != null) bossWeaponBleed.Play(true);

    }

    void OnTriggerEnter(Collider other) {
        if (other.CompareTag(targetTag)) {
            GameObject rootTarget = other.transform.root.gameObject;

            // If we already hit this exact root object during this swing, ignore it!
            if (hitList.Contains(rootTarget)) return;

            Vector3 impactPoint = other.ClosestPoint(transform.position);

            if (targetTag == "Player") {
                PlayerHealth playerHP = other.GetComponentInParent<PlayerHealth>();
                if (playerHP != null) {
                    playerHP.TakeDamage(damageAmount, impactPoint);
                    hitList.Add(rootTarget); // Remember the player
                    if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX3D("Sword_Hit_Flesh", other.transform.position, 1f, 0.2f);
                }
            } else if (targetTag == "Enemy") {
                EnemyBase enemyHP = other.GetComponentInParent<EnemyBase>();
                if (enemyHP != null) {
                    enemyHP.TakeDamage(damageAmount, impactPoint);
                    hitList.Add(rootTarget); // Remember the enemy
                    if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX3D("Sword_Hit_Flesh", other.transform.position, 1f, 0.2f);
                }

                // --- THE FIX: Use GetInParent and Add to hitList! ---
                Altar altar = other.GetComponentInParent<Altar>();
                if (altar != null) {
                    altar.TakeDamage(damageAmount);
                    hitList.Add(rootTarget); // Remember the altar!
                    if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX3D("Altar_Impact", other.transform.position, 1f, 0.2f);
                }

                // Shake the camera if we successfully hit an enemy OR an altar
                if ((enemyHP != null || altar != null) && JuiceManager.Instance != null) {
                    JuiceManager.Instance.ShakeCamera(1f);
                }
            }
        }
    }
}