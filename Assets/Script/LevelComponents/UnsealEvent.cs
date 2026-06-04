using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class UnsealEvent : MonoBehaviour, IInteractable {
    [Header("Event Settings")]
    [Tooltip("How many seconds the player must survive")]
    public float unsealDuration = 30f;
    [Tooltip("The EXACT scene name to load when the timer finishes")]
    public string nextSceneName = "Level_3";

    [Header("Spawners & UI")]
    public List<GameObject> enemySpawners;

    [Tooltip("Drag the PARENT GameObject holding your Slider (like a Panel or Canvas) here")]
    public GameObject unsealUIContainer;

    [Tooltip("Drag the actual UI Slider here")]
    public Slider unsealProgressBar;

    [Header("Talisman Settings")]
    [Tooltip("Drag the 3D model of the talisman paper here")]
    public Transform talismanPaper;
    [Tooltip("How fast the talisman bobs up and down")]
    public float floatSpeed = 2f;
    [Tooltip("How high it floats from its starting position")]
    public float floatHeight = 0.2f;

    [Header("Interaction Settings")]
    public string interactPromptText = "Begin Unsealing";

    private bool eventStarted = false;
    private float talismanStartY;

    // --- NEW: Reference to the Player ---
    private PlayerHealth playerHealth;

    void Start() {
        if (talismanPaper != null) {
            talismanStartY = talismanPaper.localPosition.y;
        }

        // --- NEW: Automatically find the player in the scene ---
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) {
            playerHealth = playerObj.GetComponent<PlayerHealth>();
        }

        foreach (GameObject spawner in enemySpawners) {
            if (spawner != null) spawner.SetActive(false);
        }

        if (unsealProgressBar != null) {
            unsealProgressBar.value = 0f;
        }

        if (unsealUIContainer != null) {
            unsealUIContainer.SetActive(false);
        } else if (unsealProgressBar != null) {
            unsealProgressBar.gameObject.SetActive(false);
        }
    }

    void Update() {
        if (talismanPaper != null) {
            Vector3 pos = talismanPaper.localPosition;
            pos.y = talismanStartY + Mathf.Sin(Time.time * floatSpeed) * floatHeight;
            talismanPaper.localPosition = pos;
        }
    }

    public void Interact() {
        if (!eventStarted) {
            eventStarted = true;
            StartCoroutine(UnsealRoutine());
        }
    }

    public string GetInteractPrompt() {
        if (eventStarted) return "";
        return interactPromptText;
    }

    private IEnumerator UnsealRoutine() {
        Debug.Log("<color=magenta>[Unseal Event] Horde defense started!</color>");

        if (AudioManager.Instance != null) {
            AudioManager.Instance.CrossfadeMusic("BGM_BossFight", 1f);
        }

        if (unsealUIContainer != null) {
            unsealUIContainer.SetActive(true);
        } else if (unsealProgressBar != null) {
            unsealProgressBar.gameObject.SetActive(true);
        }

        foreach (GameObject spawnerObj in enemySpawners) {
            if (spawnerObj != null) {
                spawnerObj.SetActive(true);
                EnemySpawner spawnerScript = spawnerObj.GetComponent<EnemySpawner>();
                if (spawnerScript != null) {
                    spawnerScript.TriggerSpawnerManually(false);
                }
            }
        }

        float timer = 0f;
        while (timer < unsealDuration) {

            // --- THE FIX: Constantly check if the player died! ---
            if (playerHealth != null && playerHealth.IsDead) {
                Debug.Log("<color=red>[Unseal Event] Player died! Aborting unseal transition.</color>");

                // Turn off the UI so it doesn't overlap the Game Over screen
                if (unsealUIContainer != null) unsealUIContainer.SetActive(false);
                else if (unsealProgressBar != null) unsealProgressBar.gameObject.SetActive(false);

                // Instantly kill this coroutine so it never reaches the SceneManager code below
                yield break;
            }

            timer += Time.deltaTime;

            if (unsealProgressBar != null) {
                unsealProgressBar.value = (timer / unsealDuration);
            }

            yield return null;
        }

        Debug.Log("<color=magenta>[Unseal Event] Unseal complete! Wiping enemies.</color>");

        foreach (GameObject spawnerObj in enemySpawners) {
            if (spawnerObj != null) spawnerObj.SetActive(false);
        }

        EnemyBase[] allEnemies = Object.FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
        foreach (EnemyBase enemy in allEnemies) {
            if (enemy != null) {
                Destroy(enemy.gameObject);
            }
        }

        if (talismanPaper != null) {
            talismanPaper.gameObject.SetActive(false);
        }

        if (GameSceneManager.Instance != null) {
            GameSceneManager.Instance.FadeScene(nextSceneName);
        } else {
            UnityEngine.SceneManagement.SceneManager.LoadScene(nextSceneName);
        }
    }
}