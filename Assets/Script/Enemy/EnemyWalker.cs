using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class EnemyWalker : EnemyBase {

    [Header("AI Settings")]
    [SerializeField] private float chaseRange = 15f;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float attackCooldown = 2f;

    [Header("Weapons")]
    public DamageHitbox bossMeleeWeapon;

    private NavMeshAgent agent;
    private Transform playerTarget;
    private float lastAttackTime;
    private bool isStunned = false;
    private bool isAttacking = false;
    private Rigidbody rb;

    protected override void Start() {
        base.Start();
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponentInChildren<Animator>();
        rb = GetComponent<Rigidbody>();
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) playerTarget = p.transform;
    }

    void Update() {
        if (playerTarget == null || isStunned || isDead || isAttacking) return;

        if (agent == null || !agent.isOnNavMesh || !agent.isActiveAndEnabled) return;

        float dist = Vector3.Distance(transform.position, playerTarget.position);

        // ==========================================
        // 1. HANDLE AGGRO & MUSIC TRIGGERS
        // ==========================================
        if (dist <= chaseRange) {
            // Player spotted! Tell the AudioManager to blast the combat BGM!
            if (!isAggroed) SetAggro(true);
        } else {
            // Player escaped the chase range!
            if (isAggroed) {
                SetAggro(false); // Fade back to calm BGM

                agent.isStopped = true; // Stop walking immediately
                if (anim != null) anim.SetBool("IsWalking", false);
            }
            return; // Stop running the rest of the code if the player is far away
        }

        // ==========================================
        // 2. HANDLE MOVEMENT & ATTACKING 
        // (This only runs if the player is inside the chase range!)
        // ==========================================
        if (dist > attackRange) {
            // We are close enough to see them, but too far to hit them. CHASE!
            agent.isStopped = false;
            agent.SetDestination(playerTarget.position);

            if (anim != null) anim.SetBool("IsWalking", agent.velocity.magnitude > 0.01f);
        } else {
            // We are inside attack range! STOP AND SWING!
            agent.isStopped = true;
            if (anim != null) anim.SetBool("IsWalking", false);

            RotateTowards(playerTarget.position);

            if (Time.time - lastAttackTime >= attackCooldown) {
                AttackPlayer();
                lastAttackTime = Time.time;
            }
        }
    }

    void RotateTowards(Vector3 target) {
        Vector3 direction = (target - transform.position).normalized;
        direction.y = 0;
        if (direction != Vector3.zero) {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 10f);
        }
    }

    void AttackPlayer() {
        if (isStunned || isDead || isAttacking) return;
        StartCoroutine(AttackRoutine());
    }

    IEnumerator AttackRoutine() {
        isAttacking = true;

        if (agent != null && agent.isOnNavMesh && agent.isActiveAndEnabled) {
            agent.isStopped = true;
        }

        if (anim != null) anim.SetTrigger("Attack");

        yield return new WaitForSeconds(0.75f);

        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX3D("Sword_Swing", transform.position, 1f, 0.1f);

        yield return new WaitForSeconds(1.75f);

        if (!isDead && agent != null && agent.isOnNavMesh && agent.isActiveAndEnabled) {
            isAttacking = false;
            agent.isStopped = false;
        } else {
            isAttacking = false;
        }
    }

    // --- UPDATED: Route damage safely ---
    public override void TakeDamage(float amount, Vector3 impactPoint = default) {
        if (isDead) return;

        // 1. This subtracts health and automatically handles Die() if health hits 0
        base.TakeDamage(amount, impactPoint);

        // 2. ONLY trigger the stun knockback if the enemy actually survived the hit!
        if (currentHealth > 0) {
            lastAttackTime = Time.time;
            TriggerKnockback();
        }
    }

    protected override void Die() {
        // --- THE FIX: Immediately shut off the weapon so the corpse isn't lethal! ---
        CloseHitbox();

        // --- THE FIX: Stop the stun routine so it doesn't accidentally revive the enemy ---
        StopAllCoroutines();

        if (agent != null && agent.isOnNavMesh) {
            agent.isStopped = true;
            agent.enabled = false;
        }

        if (rb != null) {
            rb.isKinematic = false;
            rb.useGravity = true;
        }

        // Call the base script to handle the animation, UI, and particles
        base.Die();
    }

    public void TriggerKnockback() {
        if (isStunned || isDead) return;
        StartCoroutine(StunRoutine());
    }

    IEnumerator StunRoutine() {
        isStunned = true;

        CloseHitbox();

        if (agent != null && agent.isOnNavMesh && agent.isActiveAndEnabled) {
            agent.isStopped = true;
        }

        if (anim != null) anim.SetTrigger("GetHit");

        yield return new WaitForSeconds(1.5f);

        if (!isDead && agent != null && agent.isOnNavMesh && agent.isActiveAndEnabled) {
            isAttacking = false;
            isStunned = false;
            agent.isStopped = false;
            if (anim != null) anim.SetBool("IsWalking", true);
        } else {
            isAttacking = false;
            isStunned = false;
        }
    }

    public void OpenHitbox() {
        if (bossMeleeWeapon != null) {
            bossMeleeWeapon.EnableHitbox();
        }
    }

    public void CloseHitbox() {
        if (bossMeleeWeapon != null) {
            bossMeleeWeapon.DisableHitbox();
        }
    }

    void OnDrawGizmosSelected() {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chaseRange);
    }
}