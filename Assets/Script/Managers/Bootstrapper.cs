using UnityEngine;

public class Bootstrapper : MonoBehaviour {
    [Header("Core System Prefabs")]
    public GameObject audioManagerPrefab;
    public GameObject sceneManagerPrefab;
    public GameObject saveManagerPrefab;

    [Header("Combat & Polish Prefabs")]
    public GameObject vfxManagerPrefab;
    public GameObject juiceManagerPrefab;
    public GameObject projectilePoolerPrefab;

    void Awake() {
        // --- CORE SYSTEMS ---
        if (AudioManager.Instance == null && audioManagerPrefab != null)
            Instantiate(audioManagerPrefab);

        if (GameSceneManager.Instance == null && sceneManagerPrefab != null)
            Instantiate(sceneManagerPrefab);

        if (SaveManager.Instance == null && saveManagerPrefab != null)
            Instantiate(saveManagerPrefab);

        // --- COMBAT & POLISH SYSTEMS ---
        if (VFXManager.Instance == null && vfxManagerPrefab != null)
            Instantiate(vfxManagerPrefab);

        if (JuiceManager.Instance == null && juiceManagerPrefab != null)
            Instantiate(juiceManagerPrefab);

        if (ProjectilePooler.Instance == null && projectilePoolerPrefab != null)
            Instantiate(projectilePoolerPrefab);

        Debug.Log("<color=cyan>[Bootstrapper] All Global Systems Booted.</color>");
    }

    void Start() {
        // After booting, automatically go to the Main Menu
        if (GameSceneManager.Instance != null) {
            GameSceneManager.Instance.FadeScene("MainMenu");
        }
    }
}