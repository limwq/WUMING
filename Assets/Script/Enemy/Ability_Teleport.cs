using UnityEngine;
using System.Collections;
using UnityEngine.AI;

public class Ability_Teleport : BossAbility {

    public enum TeleportStyle { RandomFlank, InPlayerFace }

    [Header("Teleport Settings")]
    public bool randomizeStyleEveryCast = true;
    public TeleportStyle defaultTeleportStyle = TeleportStyle.RandomFlank;

    [HideInInspector]
    public bool forceEscapeNextCast = false;

    [Tooltip("Used for RandomFlank")]
    [SerializeField] private float minDistance = 10f;
    [SerializeField] private float maxDistance = 15f;

    [Tooltip("Used for InPlayerFace")]
    [SerializeField] private float faceDistance = 2f;

    [Tooltip("How far the NavMesh will search to find a valid floor.")]
    [SerializeField] private float navMeshSearchRadius = 2f;

    [Header("Animation Settings")]
    [Tooltip("How long to wait after triggering the animation before actually disappearing.")]
    [SerializeField] private float preTeleportAnimDelay = 10f; // --- NEW ---

    protected override IEnumerator CastLogic() {
        if (playerTarget == null) yield break;

        // --- NEW: Play the Teleport Animation! ---
        // (Make sure "CastTeleport" matches the trigger in your Animator!)
        if (anim != null) anim.SetTrigger("CastTeleport");

        Debug.Log("<color=cyan>[Boss Teleport] Animation Triggered! Waiting for " + preTeleportAnimDelay + " seconds before teleporting.</color>");
        // Wait a split second so the player actually sees the animation before the boss vanishes
        yield return new WaitForSeconds(preTeleportAnimDelay);
        Debug.Log("<color=cyan>[Boss Teleport] Pre-teleport delay complete. Calculating position...</color>");

        Vector3 targetPos = CalculatePosition();

        if (NavMesh.SamplePosition(targetPos, out NavMeshHit hit, navMeshSearchRadius, NavMesh.AllAreas)) {

            // Now we trigger the base class blink to turn invisible and move!
            yield return StartCoroutine(ExecuteBlink(hit.position));

            Vector3 dir = (playerTarget.position - transform.root.position).normalized;
            transform.root.forward = new Vector3(dir.x, 0, dir.z);
        } else {
            Debug.LogWarning("[Boss Teleport] Failed to find valid NavMesh point. Blinking in place.");
            yield return StartCoroutine(ExecuteBlink(transform.root.position));
        }
    }

    private Vector3 CalculatePosition() {
        TeleportStyle currentStyle = defaultTeleportStyle;

        // Check the escape override flag FIRST
        if (forceEscapeNextCast) {
            currentStyle = TeleportStyle.RandomFlank;
            forceEscapeNextCast = false;
            Debug.Log("<color=yellow>[Boss Teleport] Forced Escape Override! Running away!</color>");
        } else if (randomizeStyleEveryCast) {
            currentStyle = Random.value > 0.5f ? TeleportStyle.InPlayerFace : TeleportStyle.RandomFlank;
        }

        if (currentStyle == TeleportStyle.InPlayerFace) {
            return playerTarget.position + (playerTarget.forward * faceDistance);
        } else {
            Vector2 randomDir2D = Random.insideUnitCircle.normalized;
            Vector3 randomDir = new Vector3(randomDir2D.x, 0, randomDir2D.y);
            float randomDist = Random.Range(minDistance, maxDistance);
            return playerTarget.position + (randomDir * randomDist);
        }
    }
}