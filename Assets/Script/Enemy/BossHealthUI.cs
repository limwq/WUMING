using UnityEngine;
using UnityEngine.UI;

public class BossHealthUI : MonoBehaviour {
    // SINGLETON: Allows any boss to find this UI instantly without dragging references
    public static BossHealthUI Instance { get; private set; }

    [Header("UI Elements")]
    [SerializeField] private Slider healthSlider;
    // Optional: Add a Text component for the Boss Name if you want

    private void Awake() {
        if (Instance == null) {
            Instance = this;
        } else {
            Destroy(gameObject);
        }

        // Hide the UI until the boss fight actually begins
        gameObject.SetActive(false);
    }

    public void ShowBossUI() {
        gameObject.SetActive(true);
    }

    public void UpdateHealth(float current, float max) {
        if (healthSlider != null) {
            healthSlider.maxValue = max;
            healthSlider.value = current;
        }
    }

    public void HideBossUI() {
        gameObject.SetActive(false);
    }
}