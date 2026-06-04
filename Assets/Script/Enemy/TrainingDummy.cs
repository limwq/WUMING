using System.Collections;
using UnityEngine;

// NOTICE: We inherit from EnemyBase, NOT MonoBehaviour
public class TrainingDummy : EnemyBase {
    // We override the TakeDamage function to add our special effect
    public override void TakeDamage(float damage, Vector3 impactPoint = default) {
        // 1. Run the base code (subtract health)
        base.TakeDamage(damage, impactPoint);

        // 2. Run our custom code (flash red)
        StartCoroutine(FlashColor());
    }

    private IEnumerator FlashColor() {
        Renderer rend = GetComponent<Renderer>();
        if (rend != null) {
            Color original = rend.material.color;
            rend.material.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            rend.material.color = original;
        }
    }
}