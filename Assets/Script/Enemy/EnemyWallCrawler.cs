using UnityEngine;
using System.Collections;

public class EnemyWallCrawler : EnemyBase {

    [Header("Wall Crawler Stats")]
    [SerializeField] private float chaseRange = 30f;
    [SerializeField] private float attackRange = 20f;
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float fireRate = 5f;
    [SerializeField] private float initialAttackDelay = 1.5f;

    [Header("Wall Adhesion Settings")]
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private float raycastLength = 2f;
    [Tooltip("What layers should block the crawler's forward movement? (e.g., Default, Player, Shields)")]
    [SerializeField] private LayerMask obstacleLayer;

    [Header("IK Limb Placement (Feet/Hands Only)")]
    [SerializeField] private bool enableIK = true;
    [SerializeField] private float ikRaycastLength = 1.0f;
    [SerializeField] private float limbOffset = 0.1f;

    [Header("Combat & Targeting")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private string projectilePoolTag = "EnemySpit";

    [Header("Visuals")]
    [SerializeField] private ParticleSystem mouthDripVFX;

    private Transform playerTarget;
    private Transform playerAimPoint;

    private float nextFireTime;
    private Vector3 wallNormal;
    private bool isStunned = false;
    private bool isAttacking = false;
    private bool isPlayerInRange = false;
    private bool isClimbingCorner = false;

    private Rigidbody rb;

    protected override void Start() {
        base.Start();
        anim = GetComponentInChildren<Animator>();

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) {
            playerTarget = p.transform;
            playerAimPoint = playerTarget.Find("AimPoint");
        }

        rb = GetComponent<Rigidbody>();
        if (rb != null) {
            rb.useGravity = false;
            rb.isKinematic = true;
        }

        wallNormal = transform.up;
    }

    private Vector3 GetTargetPosition() {
        return playerAimPoint != null ? playerAimPoint.position : playerTarget.position + (Vector3.up * 1f);
    }

    void FixedUpdate() {
        if (playerTarget == null || isStunned || isDead) return;

        // 1. Always stay glued to the wall! 
        // (This MUST run first so it doesn't fall off when the player runs away)
        AlignToWall();

        float dist = Vector3.Distance(transform.position, GetTargetPosition());

        // ==========================================
        // 2. HANDLE AGGRO & MUSIC TRIGGERS
        // ==========================================
        if (dist <= chaseRange) {
            // Player spotted! Blast the combat BGM!
            if (!isAggroed) SetAggro(true);
        } else {
            // Player escaped!
            if (isAggroed) {
                SetAggro(false); // Fade back to calm BGM
                if (anim != null) anim.SetBool("IsWalking", false);
            }
            // Stop running the rest of the attack/chase code, but remain glued to the wall!
            return;
        }

        // ==========================================
        // 3. COMBAT & MOVEMENT
        // ==========================================

        // If we are close enough to attack, smoothly rotate the body to track the player
        if (dist <= attackRange) {
            RotateBodyOnWall();
        }

        // If currently shooting, do not run or trigger new attacks
        if (isAttacking) return;

        // Handle Movement
        bool isMoving = false;
        if (dist <= chaseRange && dist > attackRange) {
            MoveAlongWall();
            isMoving = true;
        }
        if (anim != null) anim.SetBool("IsWalking", isMoving);

        // Handle Combat Trigger
        if (dist <= attackRange) {
            if (!isPlayerInRange) {
                isPlayerInRange = true;
                nextFireTime = Time.time + initialAttackDelay;
            }

            if (Time.time >= nextFireTime) {
                StartCoroutine(AttackRoutine());
            }
        } else {
            isPlayerInRange = false;
        }
    }

    // --- UPDATED: Standard Animation Attack Routine ---
    IEnumerator AttackRoutine() {
        isAttacking = true;

        // 1. Stop sliding and trigger the animation! Start the Anticipation Drip!
        if (mouthDripVFX != null) mouthDripVFX.Play(true);
        if (anim != null) {
            anim.SetBool("IsWalking", false);
            anim.SetTrigger("Attack");
        }

        if (!rb.isKinematic) {
            rb.linearVelocity = Vector3.zero;
        }

        //Audio
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX3D("Crawler_Spit", firePoint.position);

        // 2. WIND UP: Wait for the animation to reach the exact frame where it spits.
        // ** CHANGE THIS NUMBER to match your specific animation timing! **
        yield return new WaitForSeconds(0.5f);


        // 3. Fire the shot!

        if (firePoint != null) {
            Vector3 aimDir = (GetTargetPosition() - firePoint.position).normalized;
            Quaternion bulletRot = Quaternion.LookRotation(aimDir);

            GameObject proj = ProjectilePooler.Instance.GetProjectile(projectilePoolTag, firePoint.position, bulletRot);
            Projectile projScript = proj.GetComponent<Projectile>();
            if (projScript != null) projScript.SetShooter(transform);
        }

        // 4. RECOVER: Wait for the rest of the animation to finish
        // ** CHANGE THIS NUMBER to match how long the recovery takes **
        yield return new WaitForSeconds(0.5f);
        if (mouthDripVFX != null) mouthDripVFX.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        nextFireTime = Time.time + fireRate;
        isAttacking = false;
    }

    void AlignToWall() {
        RaycastHit hit;
        Vector3 rayOrigin = transform.position + (transform.up * 0.5f);
        Vector3 targetNormal = transform.up;
        isClimbingCorner = false;
        bool foundWall = false;

        if (Physics.Raycast(rayOrigin, transform.forward, out hit, 1.5f, wallLayer)) {
            targetNormal = hit.normal;
            isClimbingCorner = true;
            foundWall = true;
        } else if (Physics.Raycast(rayOrigin, -transform.up, out hit, raycastLength, wallLayer)) {
            targetNormal = hit.normal;
            Vector3 desiredPos = hit.point + (hit.normal * 0.3f);
            rb.MovePosition(Vector3.Lerp(rb.position, desiredPos, Time.fixedDeltaTime * 10f));
            foundWall = true;
        } else if (Physics.Raycast(rayOrigin + transform.forward, -transform.up, out hit, raycastLength, wallLayer)) {
            targetNormal = hit.normal;
            foundWall = true;
        }

        if (foundWall) {
            rb.useGravity = false;
            rb.isKinematic = true;
        } else {
            rb.useGravity = true;
            rb.isKinematic = false;
            targetNormal = Vector3.up;
        }

        wallNormal = Vector3.Lerp(wallNormal, targetNormal, Time.fixedDeltaTime * 10f).normalized;
    }

    private Vector3 GetSafeDirectionOnWall() {
        Vector3 dirToPlayer = GetTargetPosition() - transform.position;
        Vector3 lookOnWall = Vector3.ProjectOnPlane(dirToPlayer, wallNormal);

        if (lookOnWall.sqrMagnitude < 0.1f) {
            lookOnWall = Vector3.Cross(transform.right, wallNormal);
            if (lookOnWall.sqrMagnitude < 0.1f) {
                lookOnWall = transform.forward;
            }
        }
        return lookOnWall.normalized;
    }

    void MoveAlongWall() {
        Vector3 safeLookDirection = GetSafeDirectionOnWall();

        if (safeLookDirection != Vector3.zero) {
            // 1. Handle the Rotation
            Quaternion targetRotation = Quaternion.LookRotation(safeLookDirection, wallNormal);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, Time.fixedDeltaTime * 8f));

            // 2. Figure out the direction and distance we WANT to move
            Vector3 moveDirection = transform.forward;
            if (isClimbingCorner) {
                moveDirection = Vector3.Cross(transform.right, wallNormal).normalized;
            }
            float moveDistance = moveSpeed * Time.fixedDeltaTime;

            // --- THE SHIELD/OBSTACLE COLLISION FIX ---
            // Shoot a sphere slightly off the wall, looking forward.
            // If it hits a solid object (like the Boss Shield), it will NOT move.
            Vector3 castOrigin = transform.position + (transform.up * 0.5f); // Start half a meter off the wall
            float crawlerRadius = 0.4f; // Adjust this if your crawler is wider!

            // --- THE FIX: We added 'obstacleLayer' to the end of the SphereCast! ---
            if (!Physics.SphereCast(castOrigin, crawlerRadius, moveDirection, out RaycastHit hit, moveDistance + 0.1f, obstacleLayer)) {

                // The path is clear! Take the step.
                Vector3 newPos = rb.position + (moveDirection * moveDistance);
                rb.MovePosition(newPos);

            } else {
                Debug.Log($"<color=orange>[WallCrawler] Blocked by {hit.collider.name}!</color>");
            }
        }
    }

    void RotateBodyOnWall() {
        Vector3 safeLookDirection = GetSafeDirectionOnWall();

        if (safeLookDirection != Vector3.zero) {
            Quaternion targetRotation = Quaternion.LookRotation(safeLookDirection, wallNormal);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, Time.fixedDeltaTime * 5f));
        }
    }

    // --- UPDATED: Route damage safely ---
    public override void TakeDamage(float amount, Vector3 impactPoint = default) {
        if (isDead) return;

        // 1. Subtracts health and handles Die() automatically
        base.TakeDamage(amount, impactPoint);

        // 2. ONLY stun if they actually survived
        if (currentHealth > 0) {
            TriggerKnockback();
        }
    }

    protected override void Die() {
        // --- THE FIX: Stop the AttackRoutine from firing a ghost bullet! ---
        StopAllCoroutines();

        if (rb != null) {
            rb.isKinematic = false;
            rb.useGravity = true;
        }

        base.Die();
    }

    public void TriggerKnockback() {
        if (isStunned || isDead) return;

        // --- THE FIX: Nuke the attack coroutine so it forgets it was trying to shoot! ---
        StopAllCoroutines();

        StartCoroutine(StunRoutine());
    }

    IEnumerator StunRoutine() {
        isStunned = true;
        isAttacking = false; // Reset the attack lock

        if (anim != null) {
            anim.SetBool("IsWalking", false);
            anim.SetTrigger("GetHit");
        }

        yield return new WaitForSeconds(1.0f);

        if (!isDead) isStunned = false;
    }

    void OnAnimatorIK(int layerIndex) {
        if (anim == null || isDead || !enableIK) return;

        PositionLimb(AvatarIKGoal.LeftFoot);
        PositionLimb(AvatarIKGoal.RightFoot);
        PositionLimb(AvatarIKGoal.LeftHand);
        PositionLimb(AvatarIKGoal.RightHand);
    }

    void PositionLimb(AvatarIKGoal goal) {
        anim.SetIKPositionWeight(goal, 1f);
        anim.SetIKRotationWeight(goal, 1f);

        Vector3 animPos = anim.GetIKPosition(goal);
        Vector3 rayOrigin = animPos + (transform.up * 0.5f);
        RaycastHit hit;

        if (Physics.Raycast(rayOrigin, -transform.up, out hit, ikRaycastLength, wallLayer)) {
            Vector3 finalPos = hit.point + (transform.up * limbOffset);
            anim.SetIKPosition(goal, finalPos);
            Quaternion limbRotation = Quaternion.LookRotation(transform.forward, hit.normal);
            anim.SetIKRotation(goal, limbRotation);
        } else {
            anim.SetIKPositionWeight(goal, 0f);
            anim.SetIKRotationWeight(goal, 0f);
        }
    }

    void OnDrawGizmosSelected() {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chaseRange);
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, transform.forward * 2f);
    }
}