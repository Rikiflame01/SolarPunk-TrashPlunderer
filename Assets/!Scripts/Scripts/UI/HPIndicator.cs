using UnityEngine;
using UnityEngine.UI;

public class HPIndicator : MonoBehaviour
{
    [SerializeField, Tooltip("Player data ScriptableObject")]
    private PlayerData playerData;

    [SerializeField, Tooltip("Radial slider for HP display")]
    private Slider hpSlider;

    [SerializeField, Tooltip("Warning image for low HP")]
    private Image warningImage;

    [SerializeField, Tooltip("Speed of HP transition (higher = faster)")]
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
            Debug.LogError("PlayerData not assigned in HPIndicator!");
        if (hpSlider == null)
            Debug.LogError("HP Slider not assigned in HPIndicator!");
        if (warningImage == null)
            Debug.LogError("Warning Image not assigned in HPIndicator!");

        // Initialize slider
        if (hpSlider != null && playerData != null)
        {
            hpSlider.minValue = 0f;
            hpSlider.maxValue = playerData.MaxPlayerHP;
            hpSlider.value = playerData.PlayerHP;
            currentDisplayValue = playerData.PlayerHP;
            targetValue = playerData.PlayerHP;
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
            Debug.Log($"HPIndicator initialized: HP={playerData.PlayerHP}/{playerData.MaxPlayerHP}");
    }

    void Update()
    {
        if (playerData == null || hpSlider == null) return;

        // Update target value
        targetValue = playerData.PlayerHP;

        // Smoothly interpolate to target value
        if (Mathf.Abs(currentDisplayValue - targetValue) > 0.01f)
        {
            currentDisplayValue = Mathf.Lerp(currentDisplayValue, targetValue, Time.deltaTime * transitionSpeed);
            hpSlider.value = currentDisplayValue;

            if (debug)
                Debug.Log($"HPIndicator: Interpolating to HP={currentDisplayValue:F2}/{playerData.MaxPlayerHP}");
        }
        else if (currentDisplayValue != targetValue)
        {
            currentDisplayValue = targetValue;
            hpSlider.value = currentDisplayValue;

            if (debug)
                Debug.Log($"HPIndicator: HP set to {currentDisplayValue}/{playerData.MaxPlayerHP}");
        }

        // Handle warning
        UpdateWarning();
    }

    private void UpdateWarning()
    {
        if (warningImage == null || playerData == null) return;

        // Check if HP is <= 35% of max
        bool shouldWarn = playerData.PlayerHP <= playerData.MaxPlayerHP * 0.35f;

        if (shouldWarn != isWarningActive)
        {
            isWarningActive = shouldWarn;
            warningImage.gameObject.SetActive(isWarningActive);
            warningTimer = 0f;

            if (debug)
                Debug.Log($"HPIndicator: Warning {(isWarningActive ? "activated" : "deactivated")} at HP={playerData.PlayerHP}/{playerData.MaxPlayerHP}");
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