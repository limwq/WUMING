using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class VFXManager : MonoBehaviour {
    public static VFXManager Instance { get; private set; }

    [System.Serializable]
    public class VFXPool {
        public string vfxTag; // e.g., "Blood", "DeathSpirit", "SpawnSmoke"
        public GameObject prefab;
        public int poolSize = 10;
    }

    public List<VFXPool> pools;
    private Dictionary<string, Queue<GameObject>> poolDictionary;

    void Awake() {

        if (Instance == null) {
            Instance = this;

            // --- THE FIX: Tell Unity not to destroy this object when loading a new scene! ---
            DontDestroyOnLoad(gameObject);
        } else {
            Destroy(gameObject);
        }


        poolDictionary = new Dictionary<string, Queue<GameObject>>();

        foreach (VFXPool pool in pools) {
            Queue<GameObject> objectPool = new Queue<GameObject>();

            for (int i = 0; i < pool.poolSize; i++) {
                GameObject obj = Instantiate(pool.prefab, transform);
                obj.SetActive(false);
                objectPool.Enqueue(obj);
            }
            poolDictionary.Add(pool.vfxTag, objectPool);
        }
    }

    // --- MAIN SPAWN METHOD ---
    // Added 'bool autoReturn = true'. Existing scripts don't need to change, they will default to true!
    public GameObject SpawnVFX(string tag, Vector3 position, Quaternion rotation, bool autoReturn = true) {
        if (!poolDictionary.ContainsKey(tag)) {
            Debug.LogWarning($"[VFXManager] Pool '{tag}' not found!");
            return null;
        }

        if (poolDictionary[tag].Count > 0) {
            GameObject vfxToSpawn = poolDictionary[tag].Dequeue();

            vfxToSpawn.transform.position = position;
            vfxToSpawn.transform.rotation = rotation;
            vfxToSpawn.SetActive(true);

            ParticleSystem ps = vfxToSpawn.GetComponent<ParticleSystem>();
            if (ps != null) ps.Play();

            // --- THE FIX: Only auto-return if the script allows it! ---
            if (autoReturn) {
                StartCoroutine(ReturnToPoolRoutine(vfxToSpawn, tag, ps));
            }

            return vfxToSpawn;
        } else {
            GameObject prefab = pools.Find(p => p.vfxTag == tag).prefab;
            GameObject obj = Instantiate(prefab, position, rotation, transform);

            ParticleSystem ps = obj.GetComponent<ParticleSystem>();
            if (ps != null) ps.Play();

            // --- THE FIX: Only auto-return if the script allows it! ---
            if (autoReturn) {
                StartCoroutine(ReturnToPoolRoutine(obj, tag, ps));
            }

            return obj;
        }
    }

    // --- NEW: A safe way for Boss scripts to return custom VFX! ---
    public void ManualReturnToPool(string tag, GameObject vfxObject) {
        if (vfxObject == null) return;

        vfxObject.SetActive(false);
        vfxObject.transform.SetParent(transform); // Parent it back to the manager

        if (poolDictionary.ContainsKey(tag)) {
            // Prevent double-queuing bugs
            if (!poolDictionary[tag].Contains(vfxObject)) {
                poolDictionary[tag].Enqueue(vfxObject);
            }
        }
    }

    // --- HELPER METHODS FOR CLEANER CODE ---
    public void SpawnBlood(Vector3 hitPoint, Vector3 hitDirection) {
        // Rotates the blood so it splatters OUT from the wound, opposite to the sword swing
        Quaternion bloodRotation = Quaternion.LookRotation(hitDirection);
        SpawnVFX("Blood", hitPoint, bloodRotation);
    }

    public void SpawnDeathSpirit(Vector3 spawnPoint) {
        // Spirits always float straight up
        SpawnVFX("DeathSpirit", spawnPoint, Quaternion.Euler(-90, 0, 0));
    }

    private IEnumerator ReturnToPoolRoutine(GameObject vfxObject, string tag, ParticleSystem ps) {
        float waitTime = ps != null ? ps.main.duration : 2f;
        yield return new WaitForSeconds(waitTime);

        // --- THE FIX: Did this object get destroyed while we were waiting? ---
        if (vfxObject == null) {
            yield break; // Safely abort the coroutine!
        }

        vfxObject.SetActive(false);

        // Safety net: parent it back to the manager in case it was stuck to a dead enemy
        vfxObject.transform.SetParent(transform);

        if (poolDictionary.ContainsKey(tag)) {
            // Prevent double-queuing bugs just in case
            if (!poolDictionary[tag].Contains(vfxObject)) {
                poolDictionary[tag].Enqueue(vfxObject);
            }
        }
    }

    // =====================================
    // --- SCENE TRANSITION CLEANUP ---
    // =====================================
    public void StopAllVisuals() {
        // Loops through every pooled object attached to this manager and forces it off
        foreach (Transform child in transform) {
            if (child.gameObject.activeInHierarchy) {
                // If it's a particle, force it to clear instantly instead of naturally fading
                ParticleSystem ps = child.GetComponent<ParticleSystem>();
                if (ps != null) {
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }

                child.gameObject.SetActive(false);
            }
        }
    }
}