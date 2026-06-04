using UnityEngine;
using System.Collections;

public class RotatingCorridor : MonoBehaviour {
    [Header("Rotation Settings")]
    [Tooltip("The axis the corridor spins on. Usually Z (0,0,1) or X (1,0,0)")]
    public Vector3 rotationAxis = Vector3.forward;

    [Tooltip("Increased speed for a faster, more dangerous-looking spin!")]
    public float rotationSpeed = 180f;

    [Header("Alignment Settings")]
    [Tooltip("How fast it snaps into place when the player approaches")]
    public float alignSpeed = 5f;

    [Header("Audio Settings")]
    [Tooltip("Drag the AudioSource containing your looping wind sound here")]
    public AudioSource windAudioSource;

    // --- NEW: VFX Settings ---
    [Header("VFX Settings")]
    [Tooltip("Drag the child ParticleSystem here (e.g., wind streaks or dust)")]
    public ParticleSystem windParticles;

    private bool isTriggered = false;
    private Quaternion targetRotation;

    void Start() {
        // Ensure the wind starts playing and looping immediately!
        if (windAudioSource != null) {
            windAudioSource.loop = true;
            if (!windAudioSource.isPlaying) {
                windAudioSource.Play();
            }
        }

        // --- NEW: Ensure the particles are playing when the scene starts! ---
        if (windParticles != null && !windParticles.isPlaying) {
            windParticles.Play();
        }
    }

    void Update() {
        // If the player hasn't triggered it yet, keep spinning infinitely!
        if (!isTriggered) {
            transform.Rotate(rotationAxis * rotationSpeed * Time.deltaTime, Space.Self);
        }
    }

    private void OnTriggerEnter(Collider other) {
        // Only trigger if the player walks in, and only trigger ONCE
        if (!isTriggered && other.CompareTag("Player")) {
            isTriggered = true;

            if (AudioManager.Instance != null) {
                AudioManager.Instance.PlaySFX3D("Player_PerfectBlock", transform.position);
            }

            StartCoroutine(AlignCorridorRoutine());
        }
    }

    private IEnumerator AlignCorridorRoutine() {
        // Keep our current rotation for the non-spinning axes, but set the spinning axis EXACTLY to 0!
        Vector3 finalEuler = transform.localEulerAngles;

        if (rotationAxis == Vector3.forward) finalEuler.z = 0f;
        else if (rotationAxis == Vector3.right) finalEuler.x = 0f;
        else if (rotationAxis == Vector3.up) finalEuler.y = 0f;

        targetRotation = Quaternion.Euler(finalEuler);

        // Track the starting volume so we can smoothly fade it out
        float startVolume = windAudioSource != null ? windAudioSource.volume : 0f;

        // Smoothly ease into the locked 0-degree position
        float t = 0;
        Quaternion startRot = transform.localRotation;

        while (t < 1f) {
            t += Time.deltaTime * alignSpeed;

            // Use SmoothStep for a nice "heavy machinery" easing effect
            float ease = Mathf.SmoothStep(0, 1, t);

            transform.localRotation = Quaternion.Slerp(startRot, targetRotation, ease);

            // Smoothly fade out the wind audio as the corridor slows down
            if (windAudioSource != null) {
                windAudioSource.volume = Mathf.Lerp(startVolume, 0f, ease);
            }

            yield return null;
        }

        // Force it exactly to 0 just to be safe from floating-point math errors
        transform.localRotation = targetRotation;
        Debug.Log("<color=green>[Corridor] Locked perfectly at 0 degrees!</color>");

        // Completely stop the wind audio once locked
        if (windAudioSource != null) {
            windAudioSource.Stop();
        }

        // --- NEW: Stop emitting particles smoothly! ---
        if (windParticles != null) {
            // "StopEmitting" lets currently alive particles finish their lifespan naturally!
            windParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        if (AudioManager.Instance != null) {
            AudioManager.Instance.PlaySFX3D("Soul", transform.position, 1f);
        }
    }
}