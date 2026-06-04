using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio; // Required for Audio Mixer

public class SettingsMenu : MonoBehaviour {
    [Header("Audio References")]
    [SerializeField] private AudioMixer mainMixer;
    [SerializeField] private string masterParam = "MasterVol";
    [SerializeField] private string musicParam = "MusicVol";
    [SerializeField] private string sfxParam = "SFXVol";

    [Header("UI References")]
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private Button backButton;

    [Header("Sliders")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    // --- THE FIX: Let other scripts safely check if the panel is on! ---
    public bool IsMenuOpen => optionsPanel != null && optionsPanel.activeInHierarchy;

    private void Start() {
        // Setup Back Button
        if (backButton != null) {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(CloseOptions);
        }

        // Setup Sliders with Listeners
        // We use a delegate to pass the float value dynamically
        masterSlider.onValueChanged.AddListener(SetMasterVolume);
        musicSlider.onValueChanged.AddListener(SetMusicVolume);
        sfxSlider.onValueChanged.AddListener(SetSFXVolume);

        // Optional: Load saved values (Basic PlayerPrefs)
        masterSlider.value = PlayerPrefs.GetFloat("MasterVol", 1f);
        musicSlider.value = PlayerPrefs.GetFloat("MusicVol", 1f);
        sfxSlider.value = PlayerPrefs.GetFloat("SFXVol", 1f);
    }

    public void OpenOptions() {
        optionsPanel.SetActive(true);
    }

    public void CloseOptions() {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayUI("Testing_ButtonClick");

        optionsPanel.SetActive(false);

        // Save preferences when closing
        PlayerPrefs.Save();
    }

    public void SetMasterVolume(float value) {
        // Clamp the lowest value to 0.0001f to prevent the Infinity crash!
        float clampedValue = Mathf.Clamp(value, 0.0001f, 1f);
        mainMixer.SetFloat(masterParam, Mathf.Log10(clampedValue) * 20);
        PlayerPrefs.SetFloat("MasterVol", value);
    }

    public void SetMusicVolume(float value) {
        float clampedValue = Mathf.Clamp(value, 0.0001f, 1f);
        mainMixer.SetFloat(musicParam, Mathf.Log10(clampedValue) * 20);
        PlayerPrefs.SetFloat("MusicVol", value);
    }

    public void SetSFXVolume(float value) {
        float clampedValue = Mathf.Clamp(value, 0.0001f, 1f);
        mainMixer.SetFloat(sfxParam, Mathf.Log10(clampedValue) * 20);
        PlayerPrefs.SetFloat("SFXVol", value);
    }
}