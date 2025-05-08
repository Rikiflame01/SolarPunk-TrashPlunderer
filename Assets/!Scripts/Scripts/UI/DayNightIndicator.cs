using UnityEngine;
using UnityEngine.UI;

public class DayNightIndicator : MonoBehaviour
{
    [SerializeField, Tooltip("Light manager for time of day")]
    private LightManager lightManager;

    [SerializeField, Tooltip("Slider for cycle progress (0-1)")]
    private Slider timeSlider;

    [SerializeField, Tooltip("Image for sun/moon indicator")]
    private Image indicatorImage;

    [SerializeField, Tooltip("Fill image of the slider")]
    private Image fillImage;

    [SerializeField, Tooltip("Additional image for cycle transparency")]
    private Image cycleImage;

    [SerializeField, Tooltip("Sun sprite for daytime")]
    private Sprite sunSprite;

    [SerializeField, Tooltip("Moon sprite for nighttime")]
    private Sprite moonSprite;

    [SerializeField, Tooltip("Enable debug logs")]
    private bool debug = false;

    [SerializeField, Tooltip("Duration for color and alpha transitions (seconds)")]
    private float transitionDuration = 2f;

    [SerializeField, Tooltip("Duration for slider rollback transition (seconds)")]
    private float rollbackDuration = 1f;

    private readonly Color daytimeFillColor = new Color(1f, 254f / 255f, 0f, 179f / 255f); // #FFFE00B3
    private readonly Color nighttimeFillColor = Color.black; // #000000FF 
    private const float sunriseTime = 350f;
    private const float sunsetTime = 1100f;
    private const float dayDuration = sunsetTime - sunriseTime; // 750 minutes
    private const float nightDuration = 1440f - sunsetTime + sunriseTime; // 690 minutes

    private bool isDaytimeCycle;
    private bool isTransitioning;
    private float transitionStartTime;
    private Color startFillColor;
    private Color targetFillColor;
    private float startAlpha;
    private float targetAlpha;
    private bool isRollingBack;
    private float rollbackStartTime;
    private float rollbackStartValue;
    private float targetSliderValue;

    void Start()
    {
        // Validate references
        if (lightManager == null)
            Debug.LogError("LightManager not assigned in DayNightIndicator!");
        if (timeSlider == null)
            Debug.LogError("Slider not assigned in DayNightIndicator!");
        if (indicatorImage == null)
            Debug.LogError("Indicator Image not assigned in DayNightIndicator!");
        if (fillImage == null)
            Debug.LogError("Fill Image not assigned in DayNightIndicator!");
        if (cycleImage == null)
            Debug.LogError("Cycle Image not assigned in DayNightIndicator!");
        if (sunSprite == null || moonSprite == null)
            Debug.LogError("Sun or Moon sprite not assigned in DayNightIndicator!");

        // Configure slider
        if (timeSlider != null)
        {
            timeSlider.minValue = 0f;
            timeSlider.maxValue = 1f;
            timeSlider.value = 0f;
        }

        // Initialize cycle based on initial TimeOfDay
        float initialTime = lightManager != null ? lightManager.TimeOfDay : 0f;
        isDaytimeCycle = initialTime >= sunriseTime && initialTime < sunsetTime;
        isTransitioning = false;
        isRollingBack = false;

        // Set initial states
        indicatorImage.sprite = isDaytimeCycle ? sunSprite : moonSprite;
        fillImage.color = isDaytimeCycle ? daytimeFillColor : nighttimeFillColor;
        if (cycleImage != null)
        {
            Color cycleColor = cycleImage.color;
            cycleColor.a = isDaytimeCycle ? 1f : 0f;
            cycleImage.color = cycleColor;
        }
    }

    void Update()
    {
        if (lightManager == null || timeSlider == null) return;

        float timeOfDay = lightManager.TimeOfDay;

        // Determine current cycle
        bool newCycleIsDaytime = timeOfDay >= sunriseTime && timeOfDay < sunsetTime;

        // Detect cycle transition
        if (newCycleIsDaytime != isDaytimeCycle)
        {
            isDaytimeCycle = newCycleIsDaytime;
            isTransitioning = true;
            transitionStartTime = Time.time;
            startFillColor = fillImage.color;
            targetFillColor = isDaytimeCycle ? daytimeFillColor : nighttimeFillColor;
            startAlpha = cycleImage != null ? cycleImage.color.a : (isDaytimeCycle ? 0f : 1f);
            targetAlpha = isDaytimeCycle ? 1f : 0f;

            // Start slider rollback
            isRollingBack = true;
            rollbackStartTime = Time.time;
            rollbackStartValue = timeSlider.value;
            targetSliderValue = 0f;

            if (debug)
                Debug.Log($"Cycle changed to {(isDaytimeCycle ? "Daytime" : "Nighttime")} at TimeOfDay={timeOfDay}. Starting transition and rollback from SliderValue={rollbackStartValue}.");
        }

        // Calculate slider value
        float sliderValue;
        if (!isRollingBack)
        {
            if (isDaytimeCycle)
            {
                // Daytime: 350 to 1100 (750 minutes)
                sliderValue = (timeOfDay - sunriseTime) / dayDuration;
            }
            else
            {
                // Nighttime: 1100 to 1440, 0 to 350 (690 minutes)
                if (timeOfDay >= sunsetTime)
                    sliderValue = (timeOfDay - sunsetTime) / nightDuration;
                else
                    sliderValue = (timeOfDay + (1440f - sunsetTime)) / nightDuration;
            }
            sliderValue = Mathf.Clamp01(sliderValue);
        }
        else
        {
            // Interpolate slider value during rollback
            float t = (Time.time - rollbackStartTime) / rollbackDuration;
            if (t >= 1f)
            {
                t = 1f;
                isRollingBack = false;
                sliderValue = 0f;
            }
            else
            {
                sliderValue = Mathf.Lerp(rollbackStartValue, targetSliderValue, t);
            }
        }

        timeSlider.value = sliderValue;

        // Update sprite and handle transitions
        UpdateIndicator(timeOfDay);
    }

    private void UpdateIndicator(float timeOfDay)
    {
        if (indicatorImage == null || fillImage == null) return;

        // Update sprite
        indicatorImage.sprite = isDaytimeCycle ? sunSprite : moonSprite;

        // Handle smooth transitions for color and alpha
        if (isTransitioning)
        {
            float t = (Time.time - transitionStartTime) / transitionDuration;
            if (t >= 1f)
            {
                t = 1f;
                isTransitioning = false;
            }

            // Interpolate fill color
            fillImage.color = Color.Lerp(startFillColor, targetFillColor, t);

            // Interpolate cycle image alpha
            if (cycleImage != null)
            {
                Color cycleColor = cycleImage.color;
                cycleColor.a = Mathf.Lerp(startAlpha, targetAlpha, t);
                cycleImage.color = cycleColor;
            }
        }
        else
        {
            // Set final colors if not transitioning
            fillImage.color = isDaytimeCycle ? daytimeFillColor : nighttimeFillColor;
            if (cycleImage != null)
            {
                Color cycleColor = cycleImage.color;
                cycleColor.a = isDaytimeCycle ? 1f : 0f;
                cycleImage.color = cycleColor;
            }
        }

        if (debug)
        {
            float cycleAlpha = cycleImage != null ? cycleImage.color.a : -1f;
        }
    }
}