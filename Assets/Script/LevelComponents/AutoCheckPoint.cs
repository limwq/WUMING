using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AutoCheckpoint : MonoBehaviour {
    [Header("Checkpoint Setup")]
    [Tooltip("Give every checkpoint a unique name! e.g., 'Bonfire_1', 'Bonfire_2'")]
    [SerializeField] private string checkpointID = "Bonfire_01";

    [Tooltip("Drag an empty GameObject here. This is exactly where the player will respawn!")]
    [SerializeField] private Transform safeSpawnPoint;

    [Header("Visuals - Particles")]
    [SerializeField] private ParticleSystem[] fireParticles;
    [SerializeField] private ParticleSystem[] smokeParticles;

    [Header("Visuals - Lighting")]
    [SerializeField] private Light[] checkpointLights;
    [SerializeField] private float maxLightIntensity = 2.0f;
    [SerializeField] private float igniteDuration = 1.5f;

    [Header("Audio")]
    [SerializeField] private string lightUpSoundName = "CandleIgnite";

    [Header("Settings")]
    [SerializeField] private float healAmount = 100f;

    private bool isLit = false;
    private Dictionary<ParticleSystem, float> originalEmissionRates = new Dictionary<ParticleSystem, float>();

    void Start() {
        // 1. Memorize the custom particle rates
        MemorizeParticles(fireParticles);
        MemorizeParticles(smokeParticles);

        // 2. CHECK THE SAVE FILE! Is this fire already lit?
        if (SaveManager.Instance != null) {
            SaveManager.GameData data = SaveManager.Instance.LoadGame();
            if (data != null && data.litCheckpoints.Contains(checkpointID)) {
                SnapToFullyLit();
                return; // Stop the rest of the Start method!
            }
        }

        // 3. If not lit, turn everything off so it is ready to be ignited
        TurnOffParticles(fireParticles);
        TurnOffParticles(smokeParticles);
        foreach (Light l in checkpointLights) { if (l != null) l.intensity = 0f; }
    }

    // --- SEPARATED HELPER METHODS ---
    void MemorizeParticles(ParticleSystem[] systems) {
        foreach (ParticleSystem ps in systems) {
            if (ps != null) originalEmissionRates[ps] = ps.emission.rateOverTimeMultiplier;
        }
    }

    void TurnOffParticles(ParticleSystem[] systems) {
        foreach (ParticleSystem ps in systems) {
            if (ps != null) {
                var emission = ps.emission;
                emission.rateOverTimeMultiplier = 0f;
                ps.Play();
            }
        }
    }

    // --- INSTANT IGNITE (For when you load a game) ---
    void SnapToFullyLit() {
        isLit = true;
        foreach (Light l in checkpointLights) { if (l != null) l.intensity = maxLightIntensity; }

        foreach (ParticleSystem ps in fireParticles) {
            if (ps != null) {
                var emission = ps.emission;
                emission.rateOverTimeMultiplier = originalEmissionRates[ps];
                ps.Play();
            }
        }
        foreach (ParticleSystem ps in smokeParticles) {
            if (ps != null) {
                var emission = ps.emission;
                emission.rateOverTimeMultiplier = originalEmissionRates[ps];
                ps.Play();
            }
        }
    }

    void OnTriggerEnter(Collider other) {
        if (other.CompareTag("Player") && !isLit) {

            // --- THE UPGRADE: Heal the player when they ignite the checkpoint! ---
            PlayerHealth pHealth = other.GetComponent<PlayerHealth>();
            if (pHealth != null) {
                // 'healAmount' is the variable you already set up at the top of this script
                pHealth.Heal(healAmount);
            }

            ActivateCheckpoint();
        }
    }

    void ActivateCheckpoint() {
        isLit = true;
        StartCoroutine(SmoothIgniteRoutine());

        // --- THE FIX: Play audio exactly at the fire particle's position ---
        if (AudioManager.Instance != null) {
            Vector3 soundPosition = transform.position; // Default fallback to the root

            // Grab the exact 3D position of the first fire particle system!
            if (fireParticles != null && fireParticles.Length > 0 && fireParticles[0] != null) {
                soundPosition = fireParticles[0].transform.position;
            }

            AudioManager.Instance.PlaySFX3D(lightUpSoundName, soundPosition);
        }

        if (SaveManager.Instance != null) {
            // SAFETY CHECK: Make sure you assigned a spawn point!
            Vector3 savePos = safeSpawnPoint != null ? safeSpawnPoint.position : transform.position;
            SaveManager.Instance.SaveGame(savePos, healAmount, checkpointID);
        }

        Debug.Log($"<color=orange>Auto-Checkpoint '{checkpointID}' Reached & Saved!</color>");
    }

    IEnumerator SmoothIgniteRoutine() {
        float timer = 0f;

        while (timer < igniteDuration) {
            timer += Time.deltaTime;
            float progress = timer / igniteDuration;
            float smoothedProgress = Mathf.SmoothStep(0f, 1f, progress);

            foreach (Light l in checkpointLights) {
                if (l != null) l.intensity = Mathf.Lerp(0f, maxLightIntensity, smoothedProgress);
            }

            UpdateParticleEmission(fireParticles, smoothedProgress);
            UpdateParticleEmission(smokeParticles, smoothedProgress);

            yield return null;
        }

        foreach (Light l in checkpointLights) { if (l != null) l.intensity = maxLightIntensity; }
        UpdateParticleEmission(fireParticles, 1f);
        UpdateParticleEmission(smokeParticles, 1f);
    }

    private void UpdateParticleEmission(ParticleSystem[] systems, float progress) {
        foreach (ParticleSystem ps in systems) {
            if (ps != null && originalEmissionRates.ContainsKey(ps)) {
                var emission = ps.emission;
                emission.rateOverTimeMultiplier = Mathf.Lerp(0f, originalEmissionRates[ps], progress);
            }
        }
    }


}