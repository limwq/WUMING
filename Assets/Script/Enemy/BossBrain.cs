using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class BossBrain : EnemyBase {

    public enum BossState { Stage1_Ritual, Transition, Stage2_Duel, Dead }
    public BossState currentState = BossState.Stage1_Ritual;

    [Header("Weapons")]
    public DamageHitbox bossMeleeWeapon;

    [Header("Stage 1 Settings")]
    public GameObject barrierVFX;
    [SerializeField] private Altar mainBigAltar;
    public BossAbility summonAbility;

    [Header("Stage 2 Abilities")]
    public BossAbility teleportAbility;
    public BossAbility slashAbility;
    public BossAbility ghostAreaAbility;
    public BossAbility projectileAbility;
    public BossAbility thunderUltimate;

    [Header("AI Settings")]
    public float decisionInterval = 1.5f;
    [SerializeField] private float flinchImmunityWindow = 3.0f;
    private int hitsTakenDuringArmor = 0;
    [SerializeField] private int hitsToTriggerCounterAttack = 3;

    [Header("Phase Transition")]
    [Tooltip("The massive ring shockwave prefab")]
    [SerializeField] private GameObject phaseShockwavePrefab;

    // --- NEW: Cinematic Trigger ---
    [Header("Cinematic Settings")]
    [Tooltip("Drag the Stage 2 Cutscene Manager here (Only needed for the Big Altar)")]
    [SerializeField] private CutsceneManager destructionCutscene;

    private NavMeshAgent agent;
    private Transform playerTarget;

    // --- State Tracking ---
    private bool isActing = false;
    private bool hasUsedUltimate = false;
    private bool isInvulnerable = false;
    private float nextDecisionTime = 0f;
    private float lastFlinchTime = 0f;

    private BossAbility currentActiveAbility;
    private Coroutine actionWaitCoroutine;
    private Coroutine flinchCoroutine;

    private void Awake() {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponentInChildren<Animator>();

        if (agent != null) {
            agent.stoppingDistance = 3f;
        }

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) playerTarget = p.transform;
    }

    protected override void Start() {
        base.Start();

        StartBossFight();

    }

    private void Update() {
        if (anim != null && agent != null && currentState != BossState.Dead) {
            Vector3 localVelocity = transform.InverseTransformDirection(agent.velocity);
            anim.SetFloat("VelocityX", localVelocity.x);
            anim.SetFloat("VelocityZ", localVelocity.z);

            if (currentState == BossState.Stage2_Duel && !isActing && playerTarget != null) {
                agent.updateRotation = false;
                Vector3 lookDir = (playerTarget.position - transform.position).normalized;
                lookDir.y = 0;
                if (lookDir != Vector3.zero) {
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 8f);
                }
            } else {
                agent.updateRotation = true;
            }
        }
    }

    protected override void UpdateHealthUI() {
        // --- THE FIX: Tell the code to also run the slider logic from EnemyBase! ---
        base.UpdateHealthUI();
    }

    protected override void Die() {
        // --- NEW: Force the active ability to stop shooting and kill its VFX! ---
        if (currentActiveAbility != null) {
            currentActiveAbility.InterruptAbility();
            currentActiveAbility = null;
        }

        // 1. Immediately kill the Stage1/Stage2 Loops on the Brain
        StopAllCoroutines();

        // 2. Shut off the weapon so the falling body doesn't hurt the player
        CloseHitbox();

        // 3. Kill the NavMesh Agent so it stops sliding
        if (agent != null && agent.isOnNavMesh) {
            agent.isStopped = true;
            agent.enabled = false;
        }

        // 4. Drop the body to the floor
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) {
            rb.isKinematic = false;
            rb.useGravity = true;
        }

        currentState = BossState.Dead;
        base.Die();
        StartCoroutine(VictorySequence());
    }

    public void ShowWeaponMeshOnly() {
        if (bossMeleeWeapon != null) {
            //Renderer[] renderers = bossMeleeWeapon.GetComponentsInChildren<Renderer>();
            //foreach (Renderer r in renderers) {
            //    r.enabled = true;
            //}
            bossMeleeWeapon.ShowWeaponMeshOnly();
        }
    }

    public void OpenHitbox() {
        if (bossMeleeWeapon != null) {
            //Renderer[] renderers = bossMeleeWeapon.GetComponentsInChildren<Renderer>();
            //foreach (Renderer r in renderers) {
            //    r.enabled = true;
            //}
            bossMeleeWeapon.EnableHitbox();
        }
    }

    public void CloseHitbox() {
        if (bossMeleeWeapon != null) {
            bossMeleeWeapon.DisableHitbox();
            //Renderer[] renderers = bossMeleeWeapon.GetComponentsInChildren<Renderer>();
            //foreach (Renderer r in renderers) {
            //    r.enabled = false;
            //}
        }
    }

    public override void TakeDamage(float damage, Vector3 impactPoint = default) {
        if (currentState == BossState.Stage1_Ritual) return;

        if (isInvulnerable) {
            Debug.Log("<color=yellow>Boss is invincible during the Ultimate!</color>");
            return;
        }

        // Subtracts health and automatically triggers Die() if HP hits 0
        base.TakeDamage(damage, impactPoint);

        // --- SAFETY CHECK: If the hit killed the boss, stop executing! ---
        if (currentState == BossState.Dead) return;

        float hpPercent = currentHealth / maxHealth;

        if (currentState == BossState.Stage2_Duel && hpPercent <= 0.3f && !hasUsedUltimate && thunderUltimate != null) {
            Debug.Log("<color=magenta>[Boss AI] HEALTH CRITICAL! INITIATING ULTIMATE SEQUENCE!</color>");
            hasUsedUltimate = true;
            isInvulnerable = true;

            if (currentActiveAbility != null) {
                currentActiveAbility.InterruptAbility();
                currentActiveAbility = null;
            }

            if (actionWaitCoroutine != null) StopCoroutine(actionWaitCoroutine);
            if (flinchCoroutine != null) StopCoroutine(flinchCoroutine);
            CloseHitbox();

            if (anim != null) {
                anim.ResetTrigger("GetHit");
                anim.ResetTrigger("CastArea");
                anim.ResetTrigger("CastProjectile");
                anim.ResetTrigger("AttackSlash");
                anim.SetTrigger("GetHit");
            }

            StartCoroutine(DelayedUltimateCast());
            return;
        }

        bool hasSuperArmor = (Time.time < lastFlinchTime + flinchImmunityWindow) ||
                             (currentActiveAbility != null && currentActiveAbility == ghostAreaAbility);

        if (hasSuperArmor) {
            Debug.Log("<color=gray>[Boss AI] Super Armor active! Boss ignores the flinch.</color>");
            hitsTakenDuringArmor++;

            if (hitsTakenDuringArmor >= hitsToTriggerCounterAttack && !isActing) {
                if (ghostAreaAbility != null && ghostAreaAbility.CanCast()) {
                    Debug.Log("<color=red>[Boss AI] PUNISHING MASHING! TRIGGERING ANTI-MELEE BURST!</color>");
                    hitsTakenDuringArmor = 0;
                    PerformAction(ghostAreaAbility);
                } else if (teleportAbility != null && teleportAbility.CanCast()) {
                    Debug.Log("<color=yellow>[Boss AI] Escaping spam!</color>");
                    hitsTakenDuringArmor = 0;

                    Ability_Teleport tpScript = teleportAbility as Ability_Teleport;
                    if (tpScript != null) tpScript.forceEscapeNextCast = true;

                    PerformAction(teleportAbility);
                }
            }
            return;
        }

        hitsTakenDuringArmor = 0;
        lastFlinchTime = Time.time;

        if (isActing) {
            Debug.Log("<color=cyan>[Boss AI] MASSIVE STAGGER! Attack Interrupted!</color>");

            if (currentActiveAbility != null) {
                currentActiveAbility.InterruptAbility();
                currentActiveAbility = null;
            }

            if (actionWaitCoroutine != null) StopCoroutine(actionWaitCoroutine);
            if (flinchCoroutine != null) StopCoroutine(flinchCoroutine);
            CloseHitbox();

            if (anim != null) {
                anim.ResetTrigger("CastArea");
                anim.ResetTrigger("CastProjectile");
                anim.ResetTrigger("AttackSlash");
                anim.SetTrigger("GetHit");
            }

            flinchCoroutine = StartCoroutine(FlinchRecovery(3.0f));
        } else {
            Debug.Log("<color=gray>[Boss AI] Hit taken, but Boss has Super Armor!</color>");

            if (Random.value > 0.8f && teleportAbility != null && teleportAbility.CanCast() && !isActing) {
                Ability_Teleport tpScript = teleportAbility as Ability_Teleport;
                if (tpScript != null) tpScript.forceEscapeNextCast = true;
                PerformAction(teleportAbility);
            }
        }
    }

    IEnumerator FlinchRecovery(float recoveryTime) {
        isActing = true;
        if (agent != null && agent.isOnNavMesh) agent.isStopped = true;

        yield return new WaitForSeconds(recoveryTime);

        isActing = false;
        nextDecisionTime = Time.time + decisionInterval;

        if (currentState == BossState.Stage2_Duel && agent != null && agent.isOnNavMesh) {
            agent.isStopped = false;
        }
    }

    IEnumerator WaitUntilAbilityFinishes(BossAbility ability) {
        yield return null;
        yield return new WaitUntil(() => !ability.IsCasting);

        isActing = false;
        currentActiveAbility = null;
        nextDecisionTime = Time.time + decisionInterval;

        if (currentState == BossState.Stage2_Duel && agent != null && agent.isOnNavMesh) {
            agent.isStopped = false;
        }
    }

    IEnumerator DelayedUltimateCast() {
        isActing = true;
        if (agent != null && agent.isOnNavMesh) agent.isStopped = true;

        yield return new WaitForSeconds(1.0f);

        isActing = false;

        PerformAction(thunderUltimate);
        StartCoroutine(UnlockHealthAfterUltimate());
    }

    IEnumerator UnlockHealthAfterUltimate() {
        yield return null;

        if (thunderUltimate != null) {
            yield return new WaitUntil(() => !thunderUltimate.IsCasting);
        }

        isInvulnerable = false;
        Debug.Log("<color=magenta>[Boss AI] Ultimate finished. Health unlocked!</color>");
    }

    void PerformAction(BossAbility ability) {
        if (ability == null || isActing) return;
        if (agent != null && agent.isOnNavMesh) agent.isStopped = true;
        isActing = true;

        currentActiveAbility = ability;
        ability.TriggerAbility();
        actionWaitCoroutine = StartCoroutine(WaitUntilAbilityFinishes(ability));
    }

    void ExecuteStrafe() {
        float randomDirection = Random.value > 0.5f ? 1f : -1f;
        Vector3 strafeVector = transform.right * randomDirection;
        Vector3 targetPos = transform.position + (strafeVector * 4f);

        NavMeshHit hit;
        if (NavMesh.SamplePosition(targetPos, out hit, 4f, NavMesh.AllAreas)) {
            agent.SetDestination(hit.position);
        } else {
            agent.SetDestination(playerTarget.position);
        }
    }

    IEnumerator Stage1Loop() {
        Debug.Log("<color=blue>[Boss AI] Entered Stage 1: Ritual. Boss is invulnerable and focuses on summoning minions.</color>");
        if (agent != null && agent.isOnNavMesh) agent.isStopped = true;
        if (barrierVFX) barrierVFX.SetActive(true);

        while (currentState == BossState.Stage1_Ritual) {
            // --- THE FIX: We only care if the Main Altar is dead! ---
            if (mainBigAltar == null || mainBigAltar.IsDestroyed) {
                StartCoroutine(EnterStage2());
                yield break;
            }

            if (!isActing && summonAbility != null && summonAbility.CanCast()) {
                PerformAction(summonAbility);
            }

            yield return new WaitForSeconds(1f);
        }
    }

    IEnumerator EnterStage2() {
        currentState = BossState.Transition;

        if (summonAbility is Ability_Summon specificSummon) {
            specificSummon.FadeOutSpiritRing();
        }

        if (VFXManager.Instance != null) {
            // If your shield is floating slightly higher, adjust the transform.position here!
            VFXManager.Instance.SpawnVFX("ShieldShatter", transform.position, Quaternion.identity);
        }

        if (AudioManager.Instance != null) {
            AudioManager.Instance.PlaySFX3D("Barrier_Shatter", transform.position);
        }

        if (barrierVFX) barrierVFX.SetActive(false);
        if (anim != null) anim.SetTrigger("Roar");

        yield return new WaitForSeconds(2.5f);

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX3D("Boss_Roar_Phase2", transform.position); // Give it a terrifying roar!

        // Spawn the massive ring shockwave at the boss's feet
        if (phaseShockwavePrefab != null) {
            Instantiate(phaseShockwavePrefab, transform.position - (Vector3.up), Quaternion.Euler(0, 0, 0));
        }

        // --- NEW: AAA SPAWN SMOKE ---
        // Spawn the smoke at their feet/center the moment they load in
        if (VFXManager.Instance != null) {
            // Adjust the Vector3.up value depending on where your enemy pivot is!
            VFXManager.Instance.SpawnVFX("SpawnSmoke", transform.position - (Vector3.up), Quaternion.identity);
        }

        // 2. MASSIVE CAMERA SHAKE
        if (JuiceManager.Instance != null) {
            JuiceManager.Instance.ShakeCamera(10f); // The biggest shake in the game!
        }

        yield return new WaitForSeconds(3f);

        currentState = BossState.Stage2_Duel;
        if (agent != null && agent.isOnNavMesh) agent.isStopped = false;
        StartCoroutine(Stage2Loop());
    }

    IEnumerator Stage2Loop() {
        Debug.Log("<color=red>[Boss AI] Entered Stage 2: Duel. Boss is now vulnerable and uses all its abilities to fight the player!</color>");
        // Cleanup Stage 1 visuals
        if (summonAbility is Ability_Summon specificSummon) {
            specificSummon.FadeOutSpiritRing();
        }
        if (barrierVFX) barrierVFX.SetActive(false);

        // --- NEW: Wake up the NavMeshAgent since we skipped the transition! ---
        if (agent != null && agent.isOnNavMesh) {
            agent.isStopped = false;
        }

        if (AudioManager.Instance != null) {
            // This locks the music so spawned minions don't interrupt it!
            AudioManager.Instance.PlayBossMusic("BGM_Boss");
        }

        while (currentState == BossState.Stage2_Duel) {
            if (playerTarget == null || isActing || Time.time < nextDecisionTime) {
                yield return new WaitForSeconds(0.1f);
                continue;
            }

            float dist = Vector3.Distance(transform.position, playerTarget.position);

            if (dist < 5f) {
                if (Random.value > 0.4f && slashAbility != null && slashAbility.CanCast()) {
                    PerformAction(slashAbility);
                } else if (ghostAreaAbility != null && ghostAreaAbility.CanCast()) {
                    PerformAction(ghostAreaAbility);
                } else {
                    ExecuteStrafe();
                }
            } else {
                bool attacked = false;
                if (Random.value > 0.4f && teleportAbility != null && teleportAbility.CanCast()) {
                    PerformAction(teleportAbility);
                    attacked = true;
                } else if (projectileAbility != null && projectileAbility.CanCast()) {
                    PerformAction(projectileAbility);
                    attacked = true;
                }

                if (!attacked) {
                    if (Random.value > 0.5f) {
                        agent.SetDestination(playerTarget.position);
                    } else {
                        ExecuteStrafe();
                    }
                }
            }

            nextDecisionTime = Time.time + decisionInterval;
        }
    }

    // The CutsceneManager will call this exact method when the cinematic finishes!
    public void StartBossFight() {
        StartCoroutine(DelayedStateInit());
    }

    private IEnumerator DelayedStateInit() {
        // Wait exactly ONE frame to let the Animator fully wake up
        yield return null;

        // Now we take control!
        if (currentState == BossState.Stage2_Duel) {
            if (anim != null) {
                anim.Play("Locomotion");
            }
            StartCoroutine(Stage2Loop());
        } else if (currentState == BossState.Stage1_Ritual) {
            if (!isAggroed) SetAggro(true);
            StartCoroutine(Stage1Loop());
        }
    }

    private IEnumerator VictorySequence() {
        if (AudioManager.Instance != null) { 
            AudioManager.Instance.StopMusic(); 
        }

        yield return null;

        // --- NEW: Trigger the Cutscene if this is the Big Altar! ---
        if (destructionCutscene != null) {
            destructionCutscene.StartCutscene();
        }
    }
}