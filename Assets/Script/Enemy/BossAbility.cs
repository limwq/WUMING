using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI;

public abstract class BossAbility : MonoBehaviour {
    [Header("Base Settings")]
    public string abilityName;
    public float cooldown = 5f;
    public float castDuration = 2f;

    [Header("Teleport Settings")]
    [Tooltip("The tag for the VFX Manager to spawn the teleport smoke")]
    [SerializeField] private string teleportVfxTag = "BossTeleportSmoke";

    public float blinkBufferTime = 0.2f;

    protected bool isOnCooldown = false;
    protected Animator anim;
    protected Transform playerTarget;
    protected Renderer[] bossMeshes;
    public bool IsCasting { get; protected set; }

    protected Coroutine activeCoroutine;

    // --- NEW: A list to track all sounds currently playing for this ability ---
    protected List<AudioSource> activeSounds = new List<AudioSource>();

    public virtual void Awake() {
        anim = GetComponentInParent<Animator>();

        Renderer[] allMeshes = transform.root.GetComponentsInChildren<Renderer>();
        List<Renderer> filteredMeshes = new List<Renderer>();

        foreach (Renderer mesh in allMeshes) {
            if (mesh.GetComponent<DamageHitbox>() == null) {
                filteredMeshes.Add(mesh);
            }
        }
        bossMeshes = filteredMeshes.ToArray();

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) playerTarget = p.transform;
    }

    public virtual bool CanCast() => !isOnCooldown;

    public void TriggerAbility() {
        if (!CanCast()) return;
        activeCoroutine = StartCoroutine(InternalCastRoutine());
    }

    private IEnumerator InternalCastRoutine() {
        isOnCooldown = true;
        IsCasting = true;

        activeSounds.Clear(); // Clear the old list just to be safe!

        yield return StartCoroutine(CastLogic());

        IsCasting = false;
        activeSounds.Clear(); // Clean up memory when the ability finishes normally

        yield return new WaitForSeconds(cooldown);
        isOnCooldown = false;
    }

    protected abstract IEnumerator CastLogic();

    public virtual void InterruptAbility() {
        Debug.Log("<color=red>ABILITY INTERRUPTED!</color>");
        StopAllCoroutines();

        // --- NEW: Kill all active sounds immediately! ---
        foreach (AudioSource source in activeSounds) {
            if (source != null && source.isPlaying) {
                source.Stop();
                source.gameObject.SetActive(false); // Instantly return it to the audio pool
            }
        }
        activeSounds.Clear();

        IsCasting = false;
        isOnCooldown = true;

        StartCoroutine(CooldownTimer());
        ToggleBossVisuals(true);
        CleanUpOnInterrupt();
    }

    private IEnumerator CooldownTimer() {
        yield return new WaitForSeconds(cooldown);
        isOnCooldown = false;
    }

    protected virtual void CleanUpOnInterrupt() { }

    // --- NEW: The AAA Helper Method ---
    protected void PlayAbilitySound(string clipName, Vector3 position, bool is3D = true) {
        if (AudioManager.Instance != null) {
            // Pass the is3D flag down to the AudioManager!
            AudioSource source = AudioManager.Instance.PlaySFX3D(clipName, position, 1f, 0.1f, is3D);
            if (source != null) {
                activeSounds.Add(source);
            }
        }
    }

    protected IEnumerator ExecuteBlink(Vector3 targetPos) {
        NavMeshAgent agent = transform.root.GetComponent<NavMeshAgent>();

        // 1. THE VANISH
        if (VFXManager.Instance != null) {
            VFXManager.Instance.SpawnVFX(teleportVfxTag, transform.root.position, Quaternion.identity);
        }

        // --- THE UPGRADE: Uses our new helper method! ---
        PlayAbilitySound("Boss_Teleport_Out", transform.root.position);

        ToggleBossVisuals(false);

        yield return new WaitForSeconds(blinkBufferTime);

        if (agent != null) agent.Warp(targetPos);
        else transform.root.position = targetPos;

        // 3. THE REAPPEARANCE
        if (VFXManager.Instance != null) {
            VFXManager.Instance.SpawnVFX(teleportVfxTag, targetPos, Quaternion.identity);
        }

        // --- THE UPGRADE: Uses our new helper method! ---
        PlayAbilitySound("Boss_Teleport_In", transform.root.position);

        ToggleBossVisuals(true);
    }

    protected void ToggleBossVisuals(bool show) {
        if (bossMeshes == null) return;
        foreach (var mesh in bossMeshes) {
            if (mesh != null) mesh.enabled = show;
        }
    }
}