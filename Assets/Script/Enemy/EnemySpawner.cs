using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemySpawner : MonoBehaviour {

    [System.Serializable]
    public class Wave {
        public string waveName = "Wave 1";
        public List<EnemyGroup> groups;
    }

    [System.Serializable]
    public class EnemyGroup {
        public string groupName = "Enemy Group";
        public GameObject enemyPrefab;
        public Transform spawnPoint;
        public int spawnCount = 3;
        public float spawnInterval = 1.5f;
    }

    [Header("Settings")]
    public List<Wave> waves;
    public bool triggerOnce = true;
    public float timeBetweenWaves = 3f;

    // --- NEW: Modular Audio Settings ---
    [Header("Audio Settings")]
    [Tooltip("Audio played when the boss calls down lightning on the spawn point")]
    [SerializeField] private string lightningAudioTag = "Ult_ThunderStrike";
    [Tooltip("Audio played the exact moment an enemy materializes")]
    [SerializeField] private string spawnAudioTag = "Boss_Teleport_In";

    private bool hasTriggered = false;
    private List<GameObject> currentWaveEnemies = new List<GameObject>();
    private bool isSpawningRoutineActive = false;

    private void OnTriggerEnter(Collider other) {
        if (other.CompareTag("Player") && !hasTriggered) {
            if (!isSpawningRoutineActive) {
                StartCoroutine(ProcessWaves());
                if (triggerOnce) hasTriggered = true;
            }
        }
    }

    public void TriggerSpawnerManually(bool isBossSummon = false) {
        if (isSpawningRoutineActive) return;

        StartCoroutine(ProcessWaves(isBossSummon));
        if (triggerOnce) hasTriggered = true;
    }

    public bool IsBusy() {
        currentWaveEnemies.RemoveAll(enemy => enemy == null);
        return isSpawningRoutineActive || currentWaveEnemies.Count > 0;
    }

    IEnumerator ProcessWaves(bool isBossSummon = false) {
        isSpawningRoutineActive = true;

        foreach (Wave wave in waves) {
            Debug.Log($"Starting Wave: {wave.waveName}");
            currentWaveEnemies.Clear();

            // 1. SPAWN THE WAVE
            foreach (EnemyGroup group in wave.groups) {
                for (int i = 0; i < group.spawnCount; i++) {

                    // --- THE BOSS LIGHTNING WARNING ---
                    if (isBossSummon) {
                        if (VFXManager.Instance != null) {
                            // Strike exactly where the enemy is about to spawn!
                            VFXManager.Instance.SpawnVFX("SummonLightning", group.spawnPoint.position, Quaternion.Euler(-90, 0, 0));
                        }

                        // --- THE UPGRADE: 3D Lightning Audio ---
                        if (AudioManager.Instance != null && !string.IsNullOrEmpty(lightningAudioTag)) {
                            AudioManager.Instance.PlaySFX3D(lightningAudioTag, group.spawnPoint.position);
                        }

                        // Wait for the lightning to strike before the enemy appears
                        yield return new WaitForSeconds(0.4f);

                        if (JuiceManager.Instance != null) {
                            JuiceManager.Instance.ShakeCamera(0.3f);
                        }
                    }

                    // Spawn the enemy
                    SpawnEnemy(group.enemyPrefab, group.spawnPoint);

                    // Wait for the next enemy in the group (subtract the lightning delay so timing stays smooth)
                    if (i < group.spawnCount - 1) {
                        float wait = isBossSummon ? Mathf.Max(0f, group.spawnInterval - 0.4f) : group.spawnInterval;
                        yield return new WaitForSeconds(wait);
                    }
                }
            }

            // 2. WAIT FOR CLEAR
            while (currentWaveEnemies.Count > 0) {
                currentWaveEnemies.RemoveAll(enemy => enemy == null);

                currentWaveEnemies.RemoveAll(enemy => {
                    EnemyBase eb = enemy.GetComponent<EnemyBase>();
                    return eb != null && eb.currentHealth <= 0;
                });

                if (currentWaveEnemies.Count == 0) break;
                yield return null;
            }

            Debug.Log($"Wave {wave.waveName} Cleared!");

            if (waves.IndexOf(wave) < waves.Count - 1) {
                yield return new WaitForSeconds(timeBetweenWaves);
            }
        }

        Debug.Log("All Waves Complete!");
        isSpawningRoutineActive = false;
    }

    void SpawnEnemy(GameObject prefab, Transform spot) {
        if (prefab != null && spot != null) {
            GameObject newEnemy = Instantiate(prefab, spot.position, spot.rotation);
            currentWaveEnemies.Add(newEnemy);

            // --- THE FIX: Force the spawned enemy into combat immediately! ---
            EnemyBase enemyScript = newEnemy.GetComponent<EnemyBase>();
            if (enemyScript != null) {
                // This will instantly trigger AudioManager.Instance.AddAggro()
                enemyScript.SetAggro(true);
            }

            if (AudioManager.Instance != null && !string.IsNullOrEmpty(spawnAudioTag)) {
                AudioManager.Instance.PlaySFX3D(spawnAudioTag, spot.position);
            }
        }
    }

    void OnDrawGizmos() {
        Gizmos.color = new Color(0, 1, 0, 0.3f);
        BoxCollider box = GetComponent<BoxCollider>();
        if (box != null) {
            Gizmos.DrawCube(transform.position + box.center, box.size);
        }
    }
}