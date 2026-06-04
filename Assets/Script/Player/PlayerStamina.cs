using UnityEngine;
using UnityEngine.UI; // For the Slider

public class PlayerStamina : MonoBehaviour {
    [Header("Stamina Stats")]
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float regenRate = 15f;    // How fast it comes back
    [SerializeField] private float regenDelay = 1.0f;  // Wait 1s after using before regen starts

    [Header("UI Reference")]
    [SerializeField] private Slider staminaSlider;     // Drag your UI Slider here

    public float CurrentStamina { get; private set; }

    private float lastUseTime;

    void Awake() {
        CurrentStamina = maxStamina;
        if (staminaSlider != null) {
            staminaSlider.maxValue = maxStamina;
            staminaSlider.value = CurrentStamina;
        }
    }

    void Update() {
        // Regeneration Logic
        if (Time.time - lastUseTime >= regenDelay && CurrentStamina < maxStamina) {
            CurrentStamina += regenRate * Time.deltaTime;
            CurrentStamina = Mathf.Min(CurrentStamina, maxStamina);
            UpdateUI();
        }
    }

    // Returns TRUE if we have enough stamina, and consumes it
    public bool UseStamina(float amount) {
        if (CurrentStamina >= amount) {
            CurrentStamina -= amount;
            lastUseTime = Time.time; // Reset regen timer
            UpdateUI();
            return true;
        }
        // Not enough stamina
        Debug.Log("Not enough Stamina!");
        return false;
    }

    // Called every frame by things like Blocking
    // Returns FALSE if we ran out completely
    public bool DrainStamina(float amountPerSecond) {
        float amount = amountPerSecond * Time.deltaTime;

        if (CurrentStamina > 0) {
            CurrentStamina -= amount;
            lastUseTime = Time.time; // Keep resetting timer so we don't regen while draining
            UpdateUI();
            return true;
        }

        // Empty!
        CurrentStamina = 0;
        return false;
    }

    public void GainStamina(float amount) {
        CurrentStamina += amount;

        // Clamp so it doesn't go over 100%
        if (CurrentStamina > maxStamina) {
            CurrentStamina = maxStamina;
        }

        UpdateUI();
    }

    void UpdateUI() {
        if (staminaSlider != null) {
            staminaSlider.value = CurrentStamina;
        }
    }
}