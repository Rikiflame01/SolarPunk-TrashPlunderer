using UnityEngine;
using UnityEngine.UI;

public class EnergyIndicator : MonoBehaviour
{
    [SerializeField, Tooltip("Player data ScriptableObject")]
    private PlayerData playerData;

    [SerializeField, Tooltip("Slider for energy display")]
    private Slider energySlider;

    [SerializeField, Tooltip("Warning image for low energy")]
    private Image warningImage;

    [SerializeField, Tooltip("Speed of energy transition (higher = faster)")]
    private float transitionSpeed = 5f;

    [SerializeField, Tooltip("Shake amplitude for warning (pixels)")]
    private float shakeAmplitude = 5f;

    [SerializeField, Tooltip("Shake frequency for warning (Hz)")]
    private float shakeFrequency = 10f;

    [SerializeField, Tooltip("Pulse scale multiplier for warning")]
    private float pulseScale = 1.2f;

    [SerializeField, Tooltip("Pulse frequency for warning (Hz)")]
    private float pulseFrequency = 1f;

    [SerializeField, Tooltip("Warning animation interval (seconds)")]
    private float warningInterval = 1f;

    [SerializeField, Tooltip("Enable debug logs")]
    private bool debug = false;

    private float currentDisplayValue;
    private float targetValue;
    private bool isWarningActive;
    private float warningTimer;
    private Vector3 warningImageBasePosition;
    private Vector3 warningImageBaseScale;

    void Start()
    {
        // Validate references
        if (playerData == null)
            Debug.LogError("PlayerData not assigned in EnergyIndicator!");
        if (energySlider == null)
            Debug.LogError("Energy Slider not assigned in EnergyIndicator!");
        if (warningImage == null)
            Debug.LogError("Warning Image not assigned in EnergyIndicator!");

        // Initialize slider
        if (energySlider != null && playerData != null)
        {
            energySlider.minValue = 0f;
            energySlider.maxValue = playerData.MaxPlayerEnergy;
            energySlider.value = playerData.PlayerEnergy;
            currentDisplayValue = playerData.PlayerEnergy;
            targetValue = playerData.PlayerEnergy;
        }

        // Initialize warning image
        if (warningImage != null)
        {
            warningImageBasePosition = warningImage.rectTransform.localPosition;
            warningImageBaseScale = warningImage.rectTransform.localScale;
            warningImage.gameObject.SetActive(false);
            isWarningActive = false;
            warningTimer = 0f;
        }

        if (debug && playerData != null)
            Debug.Log($"EnergyIndicator initialized: Energy={playerData.PlayerEnergy}/{playerData.MaxPlayerEnergy}");
    }

    void Update()
    {
        if (playerData == null || energySlider == null) return;

        // Update target value
        targetValue = playerData.PlayerEnergy;

        // Smoothly interpolate to target value
        if (Mathf.Abs(currentDisplayValue - targetValue) > 0.01f)
        {
            currentDisplayValue = Mathf.Lerp(currentDisplayValue, targetValue, Time.deltaTime * transitionSpeed);
            energySlider.value = currentDisplayValue;

            if (debug)
                Debug.Log($"EnergyIndicator: Interpolating to Energy={currentDisplayValue:F2}/{playerData.MaxPlayerEnergy}");
        }
        else if (currentDisplayValue != targetValue)
        {
            currentDisplayValue = targetValue;
            energySlider.value = currentDisplayValue;

            if (debug)
                Debug.Log($"EnergyIndicator: Energy set to {currentDisplayValue}/{playerData.MaxPlayerEnergy}");
        }

        // Handle warning
        UpdateWarning();
    }

    private void UpdateWarning()
    {
        if (warningImage == null || playerData == null) return;

        // Check if energy is <= 35% of max
        bool shouldWarn = playerData.PlayerEnergy <= playerData.MaxPlayerEnergy * 0.35f;

        if (shouldWarn != isWarningActive)
        {
            isWarningActive = shouldWarn;
            warningImage.gameObject.SetActive(isWarningActive);
            warningTimer = 0f;

            if (debug)
                Debug.Log($"EnergyIndicator: Warning {(isWarningActive ? "activated" : "deactivated")} at Energy={playerData.PlayerEnergy}/{playerData.MaxPlayerEnergy}");
        }

        if (isWarningActive)
        {
            warningTimer += Time.deltaTime;
            bool isActivePhase = (warningTimer % (2 * warningInterval)) < warningInterval;

            if (isActivePhase)
            {
                // Shake effect
                float shakeX = Mathf.Sin(Time.time * shakeFrequency * 2f * Mathf.PI) * shakeAmplitude;
                float shakeY = Mathf.Cos(Time.time * shakeFrequency * 2f * Mathf.PI) * shakeAmplitude;
                warningImage.rectTransform.localPosition = warningImageBasePosition + new Vector3(shakeX, shakeY, 0f);

                // Pulse effect
                float pulse = 1f + Mathf.Sin(Time.time * pulseFrequency * 2f * Mathf.PI) * (pulseScale - 1f) * 0.5f;
                warningImage.rectTransform.localScale = warningImageBaseScale * pulse;
            }
            else
            {
                // Reset to base position and scale during pause
                warningImage.rectTransform.localPosition = warningImageBasePosition;
                warningImage.rectTransform.localScale = warningImageBaseScale;
            }
        }
    }
}