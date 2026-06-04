using UnityEngine;
using System.Collections;

public class Ability_Summon : BossAbility {

    [Header("Summon Settings")]
    [Tooltip("Drag your Enemy Spawners into this array.")]
    [SerializeField] private EnemySpawner[] spawners;

    [Tooltip("How long to wait for the summon animation to play before enemies actually appear.")]
    [SerializeField] private float spawnDelay = 1.0f;

    [Header("Visuals")]
    [Tooltip("Drag the BossSpiritRing particle system here")]
    [SerializeField] private ParticleSystem spiritRingVFX;
    [Tooltip("The normal emission rate when the ring is fully visible")]
    [SerializeField] private float normalEmissionRate = 30f;
    [Tooltip("How fast the ring fades in and out (in seconds)")]
    [SerializeField] private float ringFadeDuration = 1.0f;

    // --- NEW: Dedicated Audio Source for the Ring ---
    [Header("Spirit Ring Audio")]
    [Tooltip("Drag the AudioSource that lives on the Spirit Ring here")]
    [SerializeField] private AudioSource spiritRingAudio;
    [Tooltip("The normal volume of the ring when it is fully visible")]
    [SerializeField] private float normalRingVolume = 1.0f;

    public override bool CanCast() {
        if (!base.CanCast()) return false;

        if (spawners != null && spawners.Length > 0) {
            foreach (EnemySpawner spawner in spawners) {
                if (spawner != null && !spawner.IsBusy()) {
                    return true;
                }
            }
        }
        return false;
    }

    protected override IEnumerator CastLogic() {
        // Start fading out the Spirit Ring (Visuals AND Audio)!
        if (spiritRingVFX != null) {
            StartCoroutine(FadeRingEmission(0f, ringFadeDuration));
        }

        yield return new WaitForSeconds(ringFadeDuration);

        if (anim != null) {
            anim.SetTrigger("CastSummon");

            if (Random.value > 0.5f) {
                anim.SetTrigger("Blocking");
            } else {
                anim.SetTrigger("ZuoFa");
            }
        }

        yield return new WaitForSeconds(spawnDelay);

        if (spawners != null && spawners.Length > 0) {
            foreach (EnemySpawner spawner in spawners) {
                if (spawner != null) {
                    spawner.TriggerSpawnerManually(true);
                }
            }
        }

        float recoveryTime = Mathf.Max(0f, castDuration - spawnDelay);
        yield return new WaitForSeconds(recoveryTime);

        // Fade the Spirit Ring back in (Visuals AND Audio)!
        if (spiritRingVFX != null) {
            StartCoroutine(FadeRingEmission(normalEmissionRate, ringFadeDuration));
        }
    }

    // --- THE UPGRADE: Fades particle emission AND audio volume simultaneously! ---
    private IEnumerator FadeRingEmission(float targetRate, float duration) {
        if (spiritRingVFX == null) yield break;

        var emission = spiritRingVFX.emission;
        float startRate = emission.rateOverTimeMultiplier;

        // Setup the audio targets
        float startVolume = 0f;
        float targetVolume = 0f;

        if (spiritRingAudio != null) {
            startVolume = spiritRingAudio.volume;
            // If we are fading the visuals in, target the max volume. If fading out, target 0.
            targetVolume = (targetRate > 0) ? normalRingVolume : 0f;

            // If we are fading in, make sure the audio source is actually playing!
            if (targetVolume > 0 && !spiritRingAudio.isPlaying) {
                spiritRingAudio.Play();
            }
        }

        float timeElapsed = 0f;

        while (timeElapsed < duration) {
            timeElapsed += Time.deltaTime;
            float t = timeElapsed / duration;

            // Smoothly fade visuals
            emission.rateOverTimeMultiplier = Mathf.Lerp(startRate, targetRate, t);

            // Smoothly fade audio
            if (spiritRingAudio != null) {
                spiritRingAudio.volume = Mathf.Lerp(startVolume, targetVolume, t);
            }

            yield return null;
        }

        // Snap to exact target values at the end
        emission.rateOverTimeMultiplier = targetRate;

        if (spiritRingAudio != null) {
            spiritRingAudio.volume = targetVolume;

            // AAA Polish: If the volume is 0, completely stop the audio source to save CPU processing power!
            if (targetVolume == 0f) {
                spiritRingAudio.Stop();
            }
        }
    }

    public void FadeOutSpiritRing() {
        if (spiritRingVFX != null) {
            StartCoroutine(FadeRingEmission(0f, ringFadeDuration));
        }
    }
}