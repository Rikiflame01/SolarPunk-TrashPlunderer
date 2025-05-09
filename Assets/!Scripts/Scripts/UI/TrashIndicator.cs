using UnityEngine;
using UnityEngine.UI;

public class TrashIndicator : MonoBehaviour
{
    [SerializeField, Tooltip("Player data ScriptableObject")]
    private PlayerData playerData;

    [SerializeField, Tooltip("Slider for trash display")]
    private Slider trashSlider;

    [SerializeField, Tooltip("Warning image for high trash")]
    private Image warningImage;

    [SerializeField, Tooltip("Speed of trash transition (higher = faster)")]
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
            Debug.LogError("PlayerData not assigned in TrashIndicator!");
        if (trashSlider == null)
            Debug.LogError("Trash Slider not assigned in TrashIndicator!");
        if (warningImage == null)
            Debug.LogError("Warning Image not assigned in TrashIndicator!");

        // Initialize slider
        if (trashSlider != null && playerData != null)
        {
            trashSlider.minValue = 0f;
            trashSlider.maxValue = playerData.MaxPlayerStorage;
            trashSlider.value = playerData.CurrentTrash;
            currentDisplayValue = playerData.CurrentTrash;
            targetValue = playerData.CurrentTrash;
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
            Debug.Log($"TrashIndicator initialized: Trash={playerData.CurrentTrash}/{playerData.MaxPlayerStorage}");
    }

    void Update()
    {
        if (playerData == null || trashSlider == null) return;

        // Update slider max value to reflect current MaxPlayerStorage
        if (trashSlider.maxValue != playerData.MaxPlayerStorage)
        {
            trashSlider.maxValue = playerData.MaxPlayerStorage;
            if (debug)
                Debug.Log($"TrashIndicator: Updated maxValue to {playerData.MaxPlayerStorage}");
        }

        // Update target value
        targetValue = playerData.CurrentTrash;

        // Smoothly interpolate to target value
        if (Mathf.Abs(currentDisplayValue - targetValue) > 0.01f)
        {
            currentDisplayValue = Mathf.Lerp(currentDisplayValue, targetValue, Time.deltaTime * transitionSpeed);
            trashSlider.value = currentDisplayValue;

            if (debug)
                Debug.Log($"TrashIndicator: Interpolating to Trash={currentDisplayValue:F2}/{playerData.MaxPlayerStorage}");
        }
        else if (currentDisplayValue != targetValue)
        {
            currentDisplayValue = targetValue;
            trashSlider.value = currentDisplayValue;

            if (debug)
                Debug.Log($"TrashIndicator: Trash set to {currentDisplayValue}/{playerData.MaxPlayerStorage}");
        }

        // Handle warning
        UpdateWarning();
    }

    private void UpdateWarning()
    {
        if (warningImage == null || playerData == null) return;

        // Check if trash is >= 80% of max
        bool shouldWarn = playerData.CurrentTrash >= playerData.MaxPlayerStorage * 0.8f;

        if (shouldWarn != isWarningActive)
        {
            isWarningActive = shouldWarn;
            warningImage.gameObject.SetActive(isWarningActive);
            warningTimer = 0f;

            if (debug)
                Debug.Log($"TrashIndicator: Warning {(isWarningActive ? "activated" : "deactivated")} at Trash={playerData.CurrentTrash}/{playerData.MaxPlayerStorage}");
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