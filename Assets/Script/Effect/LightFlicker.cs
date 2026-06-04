using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Light))]
[RequireComponent(typeof(AudioSource))]
public class LightFlicker : MonoBehaviour {

    [Header("Optimization (Culling)")]
    [Tooltip("Check this if this light is in a menu or cutscene and should NEVER stop flickering")]
    [SerializeField] private bool ignorePlayer = false;

    [Tooltip("How close the player needs to be for the light to start flickering")]
    [SerializeField] private float activeDistance = 20f;
    private Transform player;

    [Header("Time ON (Seconds)")]
    [SerializeField] private float minOnTime = 0.5f;
    [SerializeField] private float maxOnTime = 3.0f;

    [Header("Time OFF (Seconds)")]
    [SerializeField] private float minOffTime = 0.05f;
    [SerializeField] private float maxOffTime = 0.2f;

    [Header("Audio Settings")]
    [Tooltip("The AudioSource attached to this light")]
    [SerializeField] private AudioSource myAudioSource;

    [Tooltip("OPTIONAL: Leave this blank for silent flickering, or add a clip for the Main Menu!")]
    [SerializeField] private AudioClip switchClip;

    [Tooltip("Minimum time (in seconds) between switch sounds so they don't overlap loudly")]
    [SerializeField] private float clickAudioCooldown = 0.4f;
    private float nextClickTime = 0f;

    private Light myLight;
    private float sqrActiveDistance;

    private void Start() {
        myLight = GetComponent<Light>();

        if (myAudioSource == null) {
            myAudioSource = GetComponent<AudioSource>();
        }

        if (myAudioSource != null) {
            myAudioSource.loop = true;
            // --- THE FIX: Hard-stop the audio on Frame 1 just to be safe! ---
            myAudioSource.Stop();
        }

        // Pre-calculate the squared distance to save CPU power later
        sqrActiveDistance = activeDistance * activeDistance;

        StartCoroutine(FlickerRoutine());
    }

    private IEnumerator FlickerRoutine() {
        // --- THE FIX: Wait exactly 1 frame so the Player has time to teleport/spawn! ---
        yield return null;

        while (true) {

            // ==========================================
            // OPTIMIZATION: DISTANCE CHECK
            // ==========================================
            if (!ignorePlayer) {
                // --- THE FIX: If the player spawned late, find them now! ---
                if (player == null) {
                    GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                    if (playerObj != null) player = playerObj.transform;
                }

                if (player != null) {
                    if ((transform.position - player.position).sqrMagnitude > sqrActiveDistance) {
                        // Make the light stay lit when the player is far away
                        myLight.enabled = true;

                        // Pause the continuous hum to save memory
                        if (myAudioSource.isPlaying) myAudioSource.Pause();

                        // Put the coroutine to sleep for 1 whole second before checking the distance again
                        yield return new WaitForSeconds(1.0f);
                        continue;
                    }
                }
            }

            // ==========================================
            // 1. LIGHT TURNS ON
            // ==========================================
            myLight.enabled = true;

            // Resume the continuous hum
            if (!myAudioSource.isPlaying) {
                myAudioSource.Play();
            }

            // Only plays if a clip is actually in the Inspector and cooldown is met
            if (switchClip != null && Time.time >= nextClickTime) {
                myAudioSource.PlayOneShot(switchClip);
                nextClickTime = Time.time + clickAudioCooldown;
            }

            float onWaitTime = Random.Range(minOnTime, maxOnTime);
            yield return new WaitForSeconds(onWaitTime);


            // ==========================================
            // 2. LIGHT FLASHES OFF
            // ==========================================
            myLight.enabled = false;

            // Pause the hum
            if (myAudioSource.isPlaying) {
                myAudioSource.Pause();
            }

            // Only plays if a clip is actually in the Inspector and cooldown is met
            if (switchClip != null && Time.time >= nextClickTime) {
                myAudioSource.PlayOneShot(switchClip);
                nextClickTime = Time.time + clickAudioCooldown;
            }

            float offWaitTime = Random.Range(minOffTime, maxOffTime);
            yield return new WaitForSeconds(offWaitTime);
        }
    }
}