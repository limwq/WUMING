using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI; // --- NEW: Required for the UI Slider ---

public class Altar : MonoBehaviour {

    [Header("Altar Type")]
    [Tooltip("Check this ONLY for the main center altar.")]
    public bool isBigAltar = false;

    [Header("Health")]
    public float maxHealth = 100f;
    private float currentHealth;

    [Header("Big Altar UI")]
    [Tooltip("Drag the health bar slider here. Make sure its Min is 0 and Max is 1.")]
    [SerializeField] private Slider healthBarSlider;

    [Header("Big Altar Mechanics")]
    [Tooltip("Drag the 3 small altars here. The big altar checks this list!")]
    public Altar[] linkedSmallAltars;

    [Tooltip("The visual forcefield protecting the Big Altar.")]
    public GameObject invulnerabilityShieldVFX;

    [Header("Cinematic Settings")]
    [Tooltip("Drag the Stage 2 Cutscene Manager here (Only needed for the Big Altar)")]
    [SerializeField] private CutsceneManager destructionCutscene;

    [Header("Visuals - The Model & Glow (Small Altars)")]
    [Tooltip("Drag the PARENT object holding all the separate altar meshes here")]
    [SerializeField] private GameObject altarModelParent;
    [Tooltip("The glowing color of the cracks/runes")]
    [SerializeField][ColorUsage(true, true)] private Color runeGlowColor = Color.red;
    [Tooltip("How bright it glows at 1% health")]
    [SerializeField] private float maxGlowIntensity = 5f;

    [Header("Visuals - Ambient VFX")]
    [Tooltip("List of all ambient particle systems to turn off when destroyed")]
    [SerializeField] private List<ParticleSystem> ambientVFXList;

    [Header("Visuals - The Destruction")]
    [Tooltip("The tag for the VFX Manager to spawn a massive dust/rock explosion")]
    [SerializeField] private string destructionVfxTag = "AltarExplosion";
    [Tooltip("The VFX tag for the shield breaking (e.g., ShieldShatter)")]
    [SerializeField] private string shieldShatterVfxTag = "ShieldShatter";

    [Header("Animation - Hover")]
    [Tooltip("How fast the small altars bob up and down")]
    [SerializeField] private float floatSpeed = 2f;
    [Tooltip("How high/low the small altars travel from their center point")]
    [SerializeField] private float floatAmplitude = 0.2f;

    private Vector3 startPosition;
    private List<Material> altarMaterials = new List<Material>();

    public bool IsDestroyed { get; private set; }
    public bool IsInvulnerable { get; private set; }

    private void Start() {
        currentHealth = maxHealth;
        startPosition = transform.position;

        // 1. Setup Boss Mechanics & UI
        if (isBigAltar) {
            IsInvulnerable = true;
            if (invulnerabilityShieldVFX != null) invulnerabilityShieldVFX.SetActive(true);

            // Initialize the health bar to full (1.0f)
            if (healthBarSlider != null) healthBarSlider.value = 1f;
        } else {
            IsInvulnerable = false;
        }

        // 2. Setup Glowing Runes (Only for Small Altars now)
        if (altarModelParent != null && !isBigAltar) {
            MeshRenderer[] renderers = altarModelParent.GetComponentsInChildren<MeshRenderer>();
            foreach (MeshRenderer r in renderers) {
                altarMaterials.Add(r.material);
            }
            UpdateGlow();
        }
    }

    private void Update() {
        if (!isBigAltar && !IsDestroyed) {
            float newY = startPosition.y + (Mathf.Sin(Time.time * floatSpeed) * floatAmplitude);
            transform.position = new Vector3(startPosition.x, newY, startPosition.z);
        }

        if (isBigAltar && IsInvulnerable) {
            bool allSmallAltarsDestroyed = true;

            foreach (Altar smallAltar in linkedSmallAltars) {
                if (smallAltar != null && !smallAltar.IsDestroyed) {
                    allSmallAltarsDestroyed = false;
                    break;
                }
            }

            if (allSmallAltarsDestroyed) {
                UnlockBigAltar();
            }
        }
    }

    private void UnlockBigAltar() {
        IsInvulnerable = false;

        if (AudioManager.Instance != null) {
            AudioManager.Instance.PlaySFX3D("Barrier_Shatter", transform.position);
        }

        if (invulnerabilityShieldVFX != null) invulnerabilityShieldVFX.SetActive(false);

        if (VFXManager.Instance != null && !string.IsNullOrEmpty(shieldShatterVfxTag)) {
            VFXManager.Instance.SpawnVFX(shieldShatterVfxTag, transform.position, Quaternion.identity);
        }

        if (JuiceManager.Instance != null) {
            JuiceManager.Instance.ShakeCamera(1f);
        }

        Debug.Log("<color=cyan>[Altar] All small altars destroyed! Big Altar is vulnerable!</color>");
    }

    public void TakeDamage(float damage) {
        if (IsDestroyed) return;

        if (IsInvulnerable) {
            Debug.Log("<color=yellow>[Altar] Attack deflected! The altar is invulnerable.</color>");
            return;
        }

        currentHealth -= damage;

        // --- THE UPGRADE: Split the visual feedback ---
        if (isBigAltar) {
            UpdateHealthBar();
        } else {
            UpdateGlow();
        }

        if (JuiceManager.Instance != null) {
            JuiceManager.Instance.ShakeCamera(0.2f);
        }

        if (currentHealth <= 0) {
            Die();
        }
    }

    // --- NEW: Handle the 0 to 1 Health Bar logic ---
    private void UpdateHealthBar() {
        if (healthBarSlider != null) {
            float healthPercent = currentHealth / maxHealth;
            healthBarSlider.value = Mathf.Clamp01(healthPercent);
        }
    }

    private void UpdateGlow() {
        if (altarMaterials.Count == 0 || isBigAltar) return;

        float healthPercent = currentHealth / maxHealth;
        float damagePercent = 1f - healthPercent;
        Color currentGlow = runeGlowColor * (damagePercent * maxGlowIntensity);

        foreach (Material mat in altarMaterials) {
            if (mat != null) {
                mat.SetColor("_EmissionColor", currentGlow);
                mat.EnableKeyword("_EMISSION");
            }
        }
    }

    private void Die() {
        IsDestroyed = true;
        Debug.Log($"<color=red>[Altar] {gameObject.name} Shattered!</color>");

        if (isBigAltar) {
            if (destructionCutscene != null) destructionCutscene.StartCutscene();
        }

        if (VFXManager.Instance != null && !string.IsNullOrEmpty(destructionVfxTag)) {
            VFXManager.Instance.SpawnVFX(destructionVfxTag, transform.position, Quaternion.identity);
        }

        if (altarModelParent != null) {
            MeshRenderer[] renderers = altarModelParent.GetComponentsInChildren<MeshRenderer>();
            foreach (MeshRenderer r in renderers) {
                r.enabled = false;
            }
        }

        if (ambientVFXList != null) {
            foreach (ParticleSystem ps in ambientVFXList) {
                if (ps != null) {
                    ps.gameObject.SetActive(false);
                }
            }
        }

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        if (JuiceManager.Instance != null) {
            JuiceManager.Instance.ShakeCamera(1.5f);
        }

        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX3D("Altar_Crumble", transform.position);
    }
}