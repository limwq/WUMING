using UnityEngine;

public class Projectile : MonoBehaviour {
    [Header("Settings")]
    [SerializeField] private float speed = 15f;
    [SerializeField] private float lifetime = 3f;

    [SerializeField] private int baseDamage = 15;
    private int currentDamage;

    [Header("Targeting")]
    [Tooltip("The default tag this bullet should hunt when spawned")]
    [SerializeField] private string defaultTargetTag = "Player";
    private string targetTag;

    [Header("Explosion Settings")]
    [SerializeField] private bool isExplosive = false;
    [SerializeField] private float explosionRadius = 3f;

    [Header("Visuals")]
    [SerializeField] private TrailRenderer trailVisual;
    [SerializeField] private ParticleSystem flightParticle;

    [Header("Impact VFX Tags")]
    [SerializeField] private string impactVfxTag = "TalismanSpark";
    [SerializeField] private string fizzleVfxTag = "TalismanAsh";

    // --- NEW: MODULAR AUDIO SETTINGS ---
    [Header("Audio Settings")]
    [Tooltip("Sound played when hitting an enemy or wall")]
    [SerializeField] private string impactAudioTag = "BurnOut";
    [Tooltip("Sound played when an explosive projectile detonates")]
    [SerializeField] private string explosionAudioTag = "Magic_Shoot_Explosion";
    [Tooltip("Sound played when the projectile dies of old age")]
    [SerializeField] private string fizzleAudioTag = "BurnOut";

    private Rigidbody rb;
    private bool isReflected = false;
    private Transform shooter;

    [Header("Pool Settings")]
    public string myPoolTag;

    void Awake() {
        rb = GetComponent<Rigidbody>();
    }

    void OnEnable() {
        isReflected = false;
        targetTag = defaultTargetTag;
        currentDamage = baseDamage;

        if (trailVisual != null) trailVisual.Clear();
        if (flightParticle != null) flightParticle.Play(true);

        Invoke(nameof(Fizzle), lifetime);
    }

    void OnDisable() {
        CancelInvoke();
        if (flightParticle != null) {
            flightParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    void FixedUpdate() {
        rb.MovePosition(transform.position + transform.forward * speed * Time.fixedDeltaTime);
    }

    void OnTriggerEnter(Collider other) {
        if (other.CompareTag("Projectile")) return;
        if (other.transform.root.CompareTag("Player") && !other.CompareTag("Player")) return;
        if (other.transform.root.CompareTag("Enemy") && !other.CompareTag("Enemy")) return;

        Vector3 impactPoint;

        if (other is MeshCollider meshCollider && !meshCollider.convex) {
            impactPoint = transform.position;
        } else {
            impactPoint = other.ClosestPoint(transform.position);
        }

        // --- 1. DEFENSE LOGIC ---
        if (other.CompareTag("Player") && targetTag == "Player" && !isReflected) {
            PlayerDefense defense = other.GetComponent<PlayerDefense>();

            if (defense != null) {
                if (defense.IsPerfectBlocking()) {
                    ReflectProjectile(other.transform.position);
                    defense.TriggerPerfectBlock(false, impactPoint);
                    return;
                } else if (defense.IsBlocking) {
                    Debug.Log("<color=blue>Projectile blocked normally!</color>");

                    if (VFXManager.Instance != null) {
                        Vector3 sparkDirection = (impactPoint - transform.position).normalized;
                        VFXManager.Instance.SpawnVFX("BlockSpark", impactPoint, Quaternion.LookRotation(sparkDirection));
                    }

                    SpawnImpactVFX(impactPoint);
                    DeactivateProjectile();
                    return;
                }
            }
        }

        // --- 2. HIT LOGIC ---
        if (other.CompareTag(targetTag)) {
            if (isExplosive) {
                Explode();
            } else {
                DealDamage(other.gameObject, impactPoint);
            }

            SpawnImpactVFX(impactPoint);
            DeactivateProjectile();
        }
        // --- 3. WALL/FLOOR LOGIC ---
        else if (!other.CompareTag("Player") && !other.CompareTag("Enemy")) {
            if (isExplosive) Explode();

            SpawnImpactVFX(impactPoint);
            DeactivateProjectile();
        }
    }

    void Explode() {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (Collider hitObject in hitColliders) {
            if (hitObject.CompareTag(targetTag)) {
                DealDamage(hitObject.gameObject, hitObject.transform.position);
            }
        }

        // --- THE UPGRADE: Dynamic Explosion Audio ---
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(explosionAudioTag)) {
            AudioManager.Instance.PlaySFX3D(explosionAudioTag, transform.position);
        }

        if (JuiceManager.Instance != null) {
            JuiceManager.Instance.ShakeCamera(2f);
        }
        Debug.Log("BOOM! Area Damage applied.");
    }

    public void ReflectProjectile(Vector3 reflectPoint) {
        isReflected = true;
        targetTag = "Enemy";
        currentDamage *= 2;

        if (shooter != null) {
            Vector3 shooterChest = shooter.position;
            Vector3 aimDir = (shooterChest - transform.position).normalized;
            transform.forward = aimDir;
        } else {
            transform.forward = -transform.forward;
        }

        CancelInvoke(nameof(DeactivateProjectile));
        Invoke(nameof(DeactivateProjectile), 3f);
    }

    void DealDamage(GameObject target, Vector3 impactPoint) {
        if (targetTag == "Enemy") {
            EnemyBase enemy = target.GetComponent<EnemyBase>();
            if (enemy != null) enemy.TakeDamage(currentDamage, impactPoint);

            Altar altar = target.GetComponent<Altar>();
            if (altar != null) altar.TakeDamage(currentDamage);

        } else if (targetTag == "Player") {
            PlayerHealth player = target.GetComponent<PlayerHealth>();
            if (player != null) player.TakeDamage(currentDamage, impactPoint);
        }
    }

    public void SetShooter(Transform owner) {
        shooter = owner;
    }

    private void DeactivateProjectile() {
        ProjectilePooler.Instance.ReturnToPool(myPoolTag, gameObject);
    }

    private void Fizzle() {
        if (VFXManager.Instance != null && !string.IsNullOrEmpty(fizzleVfxTag)) {
            VFXManager.Instance.SpawnVFX(fizzleVfxTag, transform.position, Quaternion.identity);
        }

        // --- THE UPGRADE: Dynamic Fizzle Audio ---
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(fizzleAudioTag)) {
            AudioManager.Instance.PlaySFX3D(fizzleAudioTag, transform.position);
        }

        DeactivateProjectile();
    }

    private void SpawnImpactVFX(Vector3 impactPoint) {
        if (VFXManager.Instance != null && !string.IsNullOrEmpty(impactVfxTag)) {
            GameObject spawnedVFX = VFXManager.Instance.SpawnVFX(impactVfxTag, impactPoint, Quaternion.LookRotation(-transform.forward));

            if (spawnedVFX != null && isExplosive) {
                spawnedVFX.transform.localScale = new Vector3(explosionRadius, explosionRadius, explosionRadius);
            }
        }

        // --- THE UPGRADE: Dynamic Impact Audio ---
        // Plays on wall hits, enemy hits, and blocks!
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(impactAudioTag)) {
            AudioManager.Instance.PlaySFX3D(impactAudioTag, impactPoint);
        }
    }

    void OnDrawGizmosSelected() {
        if (isExplosive) {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, explosionRadius);
        }
    }
}