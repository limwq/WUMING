using UnityEngine;
using TMPro; // Required for TextMeshPro

public class EnemyHealthUI : MonoBehaviour {
    [Header("References")]
    [SerializeField] private TextMeshProUGUI healthText;

    // We need a reference to the camera to face it
    private Camera mainCamera;

    void Start() {
        mainCamera = Camera.main;
    }

    // 1. Update the numbers
    public void UpdateHealthText(float current, float max) {
        if (healthText != null) {
            healthText.text = $"{current} / {max}";
        }
    }

    // 2. Billboarding (Make text always face the camera)
    void LateUpdate() {
        // Simply rotate the canvas to look at the camera
        // We add 180 degrees because UI text looks backwards if you just LookAt()
        transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward,
                         mainCamera.transform.rotation * Vector3.up);
    }
}