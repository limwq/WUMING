using UnityEngine;
using System.Collections.Generic;

public class ProjectilePooler : MonoBehaviour {
    public static ProjectilePooler Instance { get; private set; }

    // 1. A custom class to hold the settings for EACH projectile type
    [System.Serializable]
    public class Pool {
        [Tooltip("What we call this pool (e.g., 'PlayerNormal', 'EnemySpit')")]
        public string poolTag;
        public GameObject prefab;
        public int poolSize;
    }

    [Header("Multiple Pools Setup")]
    public List<Pool> pools;

    // 2. The Dictionary acts like a filing cabinet to find the right queue instantly
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

        // 3. Loop through every pool you set up in the Inspector and build them
        foreach (Pool pool in pools) {
            Queue<GameObject> objectPool = new Queue<GameObject>();

            for (int i = 0; i < pool.poolSize; i++) {
                GameObject obj = Instantiate(pool.prefab, transform);
                obj.SetActive(false);
                objectPool.Enqueue(obj); // Fill the drawer
            }

            // Label the drawer and add it to the filing cabinet!
            poolDictionary.Add(pool.poolTag, objectPool);
        }
    }

    // 4. Scripts now MUST provide a tag to get the correct bullet
    public GameObject GetProjectile(string tag, Vector3 position, Quaternion rotation) {
        if (!poolDictionary.ContainsKey(tag)) {
            Debug.LogError($"[ProjectilePooler] ERROR: Pool with tag '{tag}' doesn't exist!");
            return null;
        }

        if (poolDictionary[tag].Count > 0) {
            GameObject obj = poolDictionary[tag].Dequeue();
            obj.transform.position = position;
            obj.transform.rotation = rotation;
            obj.SetActive(true);
            return obj;
        } else {
            // Safety Net: If we run out, make a new one dynamically
            Debug.LogWarning($"[ProjectilePooler] Pool '{tag}' ran out! Spawning a new one.");
            GameObject prefab = pools.Find(p => p.poolTag == tag).prefab;
            GameObject obj = Instantiate(prefab, position, rotation, transform);
            return obj;
        }
    }

    // 5. Bullets must provide their tag to go back into the correct drawer
    public void ReturnToPool(string tag, GameObject obj) {
        obj.SetActive(false);
        if (poolDictionary.ContainsKey(tag)) {
            poolDictionary[tag].Enqueue(obj);
        }
    }
}