using UnityEngine;
using System.Collections;

public class Ability_BossSlash : BossAbility {

    // We don't need to drag the hitbox here anymore, BossBrain handles it!

    protected override IEnumerator CastLogic() {
        // 1. Face the player before swinging
        if (playerTarget != null) {
            Vector3 dir = (playerTarget.position - transform.root.position).normalized;
            transform.root.forward = new Vector3(dir.x, 0, dir.z);
        }

        // 2. Play the animation
        if (anim != null) anim.SetTrigger("AttackSlash");

        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX3D("Boss_Slash", transform.position);

        // 3. Wait for the animation to finish (e.g., 2.0 seconds)
        yield return new WaitForSeconds(castDuration);

        // --- THE FAILSAFE ---
        // If the animation event failed to fire (or the clip was missing), 
        // we forcefully close the hitbox here so the boss doesn't become a walking death-cube.
        BossBrain brain = GetComponentInParent<BossBrain>();
        if (brain != null) {
            brain.CloseHitbox();
        }
    }
}