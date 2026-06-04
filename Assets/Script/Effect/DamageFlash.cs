using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DamageFlash : MonoBehaviour {
    [Header("Flash Settings")]
    [Tooltip("Drag your pure white unlit Material here")]
    [SerializeField] private Material flashMaterial;
    [Tooltip("How long the enemy stays white")]
    [SerializeField] private float flashDuration = 0.1f;

    // A dictionary to remember EXACTLY what materials each body part had before flashing
    private Dictionary<Renderer, Material[]> originalMaterials = new Dictionary<Renderer, Material[]>();
    private Coroutine flashCoroutine;

    void Awake() {
        // Grab absolutely everything that renders on this enemy
        Renderer[] allRenderers = GetComponentsInChildren<Renderer>();

        foreach (Renderer r in allRenderers) {
            // --- THE AAA FIX ---
            // Only add it to our flash list if it is a physical 3D Mesh. 
            // This automatically ignores ParticleSystemRenderers and TrailRenderers!
            if (r is SkinnedMeshRenderer || r is MeshRenderer) {
                originalMaterials.Add(r, r.sharedMaterials);
            }
        }
    }

    public void TriggerFlash() {
        // If they get hit really fast, stop the old flash and reset before flashing again
        if (flashCoroutine != null) {
            StopCoroutine(flashCoroutine);
            ResetMaterials();
        }

        if (gameObject.activeInHierarchy) {
            flashCoroutine = StartCoroutine(FlashRoutine());
        }
    }

    private IEnumerator FlashRoutine() {
        // 1. Swap every valid mesh to the white flash material
        foreach (KeyValuePair<Renderer, Material[]> entry in originalMaterials) {
            if (entry.Key != null) {
                // We have to make an array of flash materials equal to the number of materials the mesh uses
                Material[] flashMats = new Material[entry.Value.Length];
                for (int i = 0; i < flashMats.Length; i++) {
                    flashMats[i] = flashMaterial;
                }
                entry.Key.materials = flashMats;
            }
        }

        // 2. Wait a split second
        yield return new WaitForSeconds(flashDuration);

        // 3. Swap back
        ResetMaterials();
    }

    private void ResetMaterials() {
        // Restore the original materials to every body part
        foreach (KeyValuePair<Renderer, Material[]> entry in originalMaterials) {
            if (entry.Key != null) {
                entry.Key.materials = entry.Value;
            }
        }
    }
}