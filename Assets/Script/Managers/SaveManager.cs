using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class SaveManager : MonoBehaviour {
    public static SaveManager Instance { get; private set; }

    [System.Serializable]
    public class GameData {
        public string sceneName;
        public Vector3 playerPosition;
        public float currentHealth;

        // Only tracking the essentials now!
        public List<string> litCheckpoints = new List<string>();
        public List<string> playedCutscenes = new List<string>();
    }

    void Awake() {
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        } else {
            Destroy(gameObject);
        }
    }

    public void SaveGame(Vector3 safePosition, float health, string checkpointID) {
        GameData data = LoadGame();
        if (data == null) data = new GameData();

        data.sceneName = SceneManager.GetActiveScene().name;
        data.playerPosition = safePosition;
        data.currentHealth = health;

        if (!data.litCheckpoints.Contains(checkpointID)) {
            data.litCheckpoints.Add(checkpointID);
        }

        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString("BonfireSave", json);
        PlayerPrefs.Save();
    }

    public void SaveWorldEvent(string eventType, string id) {
        if (string.IsNullOrEmpty(id)) return;

        GameData data = LoadGame();
        if (data == null) data = new GameData();

        // Only keeping the Cutscene check!
        if (eventType == "Cutscene" && !data.playedCutscenes.Contains(id)) {
            data.playedCutscenes.Add(id);

            string json = JsonUtility.ToJson(data);
            PlayerPrefs.SetString("BonfireSave", json);
            PlayerPrefs.Save();
        }
    }

    public GameData LoadGame() {
        if (PlayerPrefs.HasKey("BonfireSave")) {
            string json = PlayerPrefs.GetString("BonfireSave");
            return JsonUtility.FromJson<GameData>(json);
        }
        return null;
    }

    public void ClearSaveData() {
        if (PlayerPrefs.HasKey("BonfireSave")) {
            PlayerPrefs.DeleteKey("BonfireSave");
            PlayerPrefs.Save();
            Debug.Log("<color=red>[SaveManager] Save Data completely wiped!</color>");
        }
    }
}