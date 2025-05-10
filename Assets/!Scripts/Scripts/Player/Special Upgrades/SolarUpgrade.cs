using UnityEngine;

public class SolarUpgrade : MonoBehaviour
{
    [SerializeField, Tooltip("Solar panel prefabs to activate on upgrade")]
    private GameObject[] solarPrefab;

    [SerializeField, Tooltip("Player data ScriptableObject for energy management")]
    private PlayerData playerData;

    [SerializeField, Tooltip("Light manager for time of day")]
    private LightManager lightManager;

    [SerializeField, Tooltip("Enable debug logs for solar energy regeneration")]
    private bool debug = false;

    private bool isSolarPanelActive = false;
    private float energyRegenAccumulator = 0f;

    private void OnEnable()
    {
        ActionManager.OnSolarPanelUpgrade += OnSolarPanelUpgrade;
    }

    private void OnDisable()
    {
        ActionManager.OnSolarPanelUpgrade -= OnSolarPanelUpgrade;
    }

    private void Start()
    {
        // Validate references
        if (playerData == null)
            Debug.LogError("PlayerData not assigned in SolarUpgrade!");
        if (lightManager == null)
            Debug.LogError("LightManager not assigned in SolarUpgrade!");
        if (solarPrefab == null || solarPrefab.Length == 0)
            Debug.LogError("SolarPrefab array not assigned or empty in SolarUpgrade!");

        // Ensure solar panels are initially inactive
        foreach (GameObject solar in solarPrefab)
        {
            if (solar != null)
                solar.SetActive(false);
        }
    }

    private void Update()
    {
        if (!isSolarPanelActive || playerData == null || lightManager == null)
            return;

        // Regenerate energy during GamePlay state and daytime (350 ≤ TimeOfDay < 1100)
        if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.GamePlay &&
            lightManager.TimeOfDay >= 350f && lightManager.TimeOfDay < 1100f)
        {
            energyRegenAccumulator += playerData.EnergyRegenRate * Time.deltaTime;
            int energyToAdd = (int)energyRegenAccumulator;
            if (energyToAdd > 0)
            {
                int newEnergy = Mathf.Min(playerData.PlayerEnergy + energyToAdd, playerData.MaxPlayerEnergy);
                energyToAdd = newEnergy - playerData.PlayerEnergy; // Adjust for max cap
                playerData.PlayerEnergy = newEnergy;
                energyRegenAccumulator -= energyToAdd;

                if (debug && energyToAdd > 0)
                {
                    Debug.Log($"SolarUpgrade: Regenerated Energy +{energyToAdd}. Current: Energy={playerData.PlayerEnergy}, TimeOfDay={lightManager.TimeOfDay}");
                }
            }
        }
    }

    private void OnSolarPanelUpgrade()
    {
        // Activate solar panel prefabs
        foreach (GameObject solar in solarPrefab)
        {
            if (solar != null)
                solar.SetActive(true);
            else
                Debug.LogWarning("Null solar prefab found in SolarUpgrade!");
        }

        // Enable energy regeneration
        isSolarPanelActive = true;

        if (debug)
            Debug.Log("Solar panel upgrade activated: Energy regeneration enabled during daytime in GamePlay state.");
    }
}