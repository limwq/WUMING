using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class AudioManager : MonoBehaviour {
    public static AudioManager Instance { get; private set; }

    [Header("Global Audio Sources")]
    [Tooltip("Requires TWO audio sources to crossfade between songs")]
    [SerializeField] private AudioSource musicSourceA;
    [SerializeField] private AudioSource musicSourceB;
    [SerializeField] private AudioSource uiSource; // Dedicated 2D source for UI

    [Header("3D Audio Pool Settings")]
    [Tooltip("How many sounds can play at the exact same time")]
    [SerializeField] private int sfxPoolSize = 15;
    [SerializeField] private GameObject sfxSourcePrefab; // An empty prefab with an AudioSource component

    [Header("Clips Library")]
    public List<AudioClip> clipList;
    private Dictionary<string, AudioClip> clipDict = new Dictionary<string, AudioClip>();

    // --- THE UPGRADE: Track the exact time a sound was last played ---
    private Dictionary<string, float> lastPlayedTimes = new Dictionary<string, float>();
    [Tooltip("How many seconds must pass before the exact same sound can play again (prevents ear-rape)")]
    [SerializeField] private float soundCooldown = 0.05f;

    // The Pool
    private List<AudioSource> sfxPool = new List<AudioSource>();

    // --- Dynamic BGM State Tracking ---
    [Header("Dynamic BGM")]
    public string explorationMusicTag = "BGM_Normal";
    public string combatMusicTag = "BGM_Fight";

    private int enemiesInCombat = 0;
    private Coroutine crossfadeRoutine;
    private bool isBossFightActive = false;

    private AudioSource currentBgmSource;

    void Awake() {
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Initialize();
        } else {
            Destroy(gameObject);
        }

        uiSource.ignoreListenerPause = true;
    }

    private void Initialize() {
        currentBgmSource = musicSourceA;

        // 1. Build the Dictionary
        foreach (var clip in clipList) {
            if (!clipDict.ContainsKey(clip.name))
                clipDict.Add(clip.name, clip);
        }

        // 2. Build the Object Pool for 3D sounds
        if (sfxSourcePrefab != null) {
            for (int i = 0; i < sfxPoolSize; i++) {
                GameObject obj = Instantiate(sfxSourcePrefab, transform);
                AudioSource source = obj.GetComponent<AudioSource>();
                source.spatialBlend = 1f; // Force it to be 3D!
                source.rolloffMode = AudioRolloffMode.Linear;
                source.minDistance = 2f;
                source.maxDistance = 30f; // Sounds fade out at 30 meters
                obj.SetActive(false);
                sfxPool.Add(source);
            }
        } else {
            Debug.LogError("[AudioManager] Missing SFX Source Prefab! Create an empty object with an AudioSource and drag it in.");
        }
    }

    // =====================================
    // --- 1. SFX & UI METHODS ---
    // =====================================

    public void PlayUI(string clipName, float volume = 1f) {
        if (string.IsNullOrEmpty(clipName)) return;

        if (clipDict.TryGetValue(clipName, out AudioClip clip)) {
            uiSource.PlayOneShot(clip, volume);
        } else {
            Debug.LogWarning($"[Audio] Clip '{clipName}' not found!");
        }
    }

    public AudioSource PlaySFX3D(string clipName, Vector3 position, float volume = 1f, float pitchVariation = 0.1f, bool is3D = true) {

        // --- THE UPGRADE: Anti-Spam Check ---
        if (lastPlayedTimes.ContainsKey(clipName)) {
            // If the sound was played less than 0.05 seconds ago, block it!
            if (Time.time - lastPlayedTimes[clipName] < soundCooldown) {
                return null;
            }
        }
        // Update the dictionary with the current time
        lastPlayedTimes[clipName] = Time.time;

        // ... Keep the rest of your exact PlaySFX3D logic here ...
        if (!clipDict.TryGetValue(clipName, out AudioClip clip)) {
            Debug.LogWarning($"[Audio] Clip '{clipName}' not found!");
            return null;
        }

        AudioSource availableSource = GetAvailableSFXSource();

        if (availableSource != null) {
            availableSource.gameObject.SetActive(true);
            availableSource.transform.position = position;
            availableSource.clip = clip;
            availableSource.volume = volume;
            availableSource.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
            availableSource.spatialBlend = is3D ? 1f : 0f;

            availableSource.Play();
            StartCoroutine(ReturnSourceToPool(availableSource, clip.length));

            return availableSource;
        } else {
            // Because of the Anti-Spam, you will almost NEVER see this warning anymore!
            Debug.LogWarning("[Audio] SFX Pool is full!");
            return null;
        }
    }

    // =====================================
    // --- 2. AAA DYNAMIC MUSIC SYSTEM ---
    // =====================================

    public void CrossfadeMusic(string clipName, float fadeDuration = 1.5f) {
        if (!clipDict.TryGetValue(clipName, out AudioClip newClip)) {
            Debug.LogWarning($"[Audio] Music '{clipName}' not found!");
            return;
        }

        // If the current official speaker is already playing this song, do nothing!
        if (currentBgmSource != null && currentBgmSource.clip == newClip) return;

        // The speaker fading out is whatever the current one is
        AudioSource activeOut = currentBgmSource;

        // The speaker fading in is the opposite one
        AudioSource idleIn = (currentBgmSource == musicSourceA) ? musicSourceB : musicSourceA;

        // IMMEDIATELY update the official tracker so future interruptions know who is boss
        currentBgmSource = idleIn;

        if (crossfadeRoutine != null) StopCoroutine(crossfadeRoutine);
        crossfadeRoutine = StartCoroutine(FadeRoutine(activeOut, idleIn, newClip, fadeDuration));
    }

    private IEnumerator FadeRoutine(AudioSource activeOut, AudioSource idleIn, AudioClip newClip, float duration) {
        if (idleIn == null || activeOut == null) yield break;

        // Setup the incoming track
        idleIn.clip = newClip;
        idleIn.volume = 0f;
        idleIn.Play();

        float startVolumeOut = activeOut.volume;
        float targetVolumeIn = 1f; // (Later, this hooks into your AudioMixer volume!)
        float timer = 0f;

        while (timer < duration) {
            timer += Time.deltaTime;
            float t = timer / duration;

            activeOut.volume = Mathf.Lerp(startVolumeOut, 0f, t);
            idleIn.volume = Mathf.Lerp(0f, targetVolumeIn, t);

            yield return null;
        }

        // Snap and cleanup
        activeOut.volume = 0f;
        activeOut.Stop();
        idleIn.volume = targetVolumeIn;
    }

    // =====================================
    // --- 3. COMBAT & BOSS LOGIC ---
    // =====================================

    private Coroutine aggroCooldownRoutine;

    public void AddAggro() {
        if (isBossFightActive) return;

        // If we were about to turn the music off, cancel it!
        if (aggroCooldownRoutine != null) {
            StopCoroutine(aggroCooldownRoutine);
            aggroCooldownRoutine = null;
        }

        enemiesInCombat++;
        if (enemiesInCombat == 1) {
            CrossfadeMusic(combatMusicTag, 1.0f);
        }
    }

    public void RemoveAggro() {
        if (isBossFightActive) return;

        enemiesInCombat--;

        // Safety clamp
        if (enemiesInCombat < 0) enemiesInCombat = 0;

        if (enemiesInCombat == 0) {
            // --- THE FIX: Wait 2 seconds before officially declaring combat over! ---
            if (aggroCooldownRoutine != null) StopCoroutine(aggroCooldownRoutine);
            aggroCooldownRoutine = StartCoroutine(AggroCooldownDelay());
        }
    }

    private IEnumerator AggroCooldownDelay() {
        // Wait for 2 seconds. If AddAggro() is called during this time, this coroutine gets killed!
        yield return new WaitForSeconds(2.0f);

        // If we made it this far, combat is truly over. Fade back to exploration music.
        CrossfadeMusic(explorationMusicTag, 3.0f);
    }

    public void PlayBossMusic(string bossMusicTag) {
        isBossFightActive = true;
        CrossfadeMusic(bossMusicTag, 2.0f);
    }

    public void ClearBossMusic() {
        isBossFightActive = false;
        enemiesInCombat = 0;
        CrossfadeMusic(explorationMusicTag, 4.0f);
    }

    // =====================================
    // --- HELPER METHODS ---
    // =====================================

    private AudioSource GetAvailableSFXSource() {
        foreach (var source in sfxPool) {
            if (!source.gameObject.activeInHierarchy) {
                return source;
            }
        }
        return null;
    }

    private IEnumerator ReturnSourceToPool(AudioSource source, float delay) {
        yield return new WaitForSeconds(delay);
        if (source != null) {
            source.Stop();
            source.gameObject.SetActive(false);
        }
    }

    // =====================================
    // --- SCENE TRANSITION CLEANUP ---
    // =====================================
    public void StopAllSFX() {
        foreach (var source in sfxPool) {
            if (source != null && source.gameObject.activeInHierarchy) {
                source.Stop();
                source.gameObject.SetActive(false);
            }
        }
    }

    public void StopMusic() {
        // Replace 'bgmSource' with whatever you named your background music AudioSource variable!
        if (musicSourceA != null && musicSourceA.isPlaying) {
            musicSourceA.Stop();
        }
        if (musicSourceB != null && musicSourceB.isPlaying) {
            musicSourceB.Stop();
        }
    }
}