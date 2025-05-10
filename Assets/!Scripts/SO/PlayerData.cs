using UnityEngine;

public enum UnlockLevel
{
    Level1,
    Level2,
    Level3
}

[CreateAssetMenu(fileName = "PlayerData", menuName = "ScriptableObjects/PlayerData", order = 1)]
public class PlayerData : ScriptableObject
{
    [Header("Player Stats")]
    [SerializeField] private int playerHP = 100;
    [SerializeField] private int maxPlayerHP = 100;
    [SerializeField] private int playerEnergy = 10;
    [SerializeField] private int maxPlayerEnergy = 10;
    [SerializeField] private int playerSpeed = 5;
    [SerializeField] private int maxPlayerSpeed = 5;
    [SerializeField] private int maxPlayerStorage = 5;
    [SerializeField] private int currentTrash = 0;
    [SerializeField] private int recyclePoints = 0;

    [Header("Special Unlocks")]
    [SerializeField] private bool trashNetUnlocked = false;
    [SerializeField] private UnlockLevel trashNetLevel = UnlockLevel.Level1;
    [SerializeField] private bool iceBreakerUnlocked = false;
    [SerializeField] private UnlockLevel iceBreakerLevel = UnlockLevel.Level1;
    [SerializeField] private bool speedBurstUnlocked = false;
    [SerializeField] private UnlockLevel speedBurstLevel = UnlockLevel.Level1;
    [SerializeField] private bool magnetUnlocked = false;
    [SerializeField] private bool solarPanelUnlocked = false;

    [Header("Upgrade Levels")]
    [SerializeField] private int speedUpgradeLevel = 0; // 0 to 3
    [SerializeField] private int storageUpgradeLevel = 0; // 0 to 4, then additional upgrades
    [SerializeField] private int energyUpgradeLevel = 0; // 0 to 4
    [SerializeField] private int maxLifeUpgradeLevel = 0; // 0 to 4
    [SerializeField] private int enginePowerLevel = 0; // 0 to 3

    #region Properties
    public int PlayerHP
    {
        get => playerHP;
        set => playerHP = Mathf.Clamp(value, 0, maxPlayerHP);
    }

    public int MaxPlayerHP
    {
        get => maxPlayerHP;
        set => maxPlayerHP = Mathf.Max(0, value);
    }

    public int PlayerEnergy
    {
        get => playerEnergy;
        set => playerEnergy = Mathf.Clamp(value, 0, maxPlayerEnergy);
    }

    public int MaxPlayerEnergy
    {
        get => maxPlayerEnergy;
        set => maxPlayerEnergy = Mathf.Max(0, value);
    }

    public int PlayerSpeed
    {
        get => playerSpeed;
        set => playerSpeed = Mathf.Max(0, value);
    }

    public int MaxPlayerSpeed
    {
        get => maxPlayerSpeed;
        set => maxPlayerSpeed = Mathf.Max(0, value);
    }

    public int MaxPlayerStorage
    {
        get => maxPlayerStorage;
        set => maxPlayerStorage = Mathf.Max(0, value);
    }

    public int CurrentTrash
    {
        get => currentTrash;
        set => currentTrash = Mathf.Max(0, value);
    }

    public int RecyclePoints
    {
        get => recyclePoints;
        set => recyclePoints = Mathf.Max(0, value);
    }

    public bool TrashNetUnlocked
    {
        get => trashNetUnlocked;
        set => trashNetUnlocked = value;
    }

    public bool IceBreakerUnlocked
    {
        get => iceBreakerUnlocked;
        set => iceBreakerUnlocked = value;
    }

    public bool SpeedBurstUnlocked
    {
        get => speedBurstUnlocked;
        set => speedBurstUnlocked = value;
    }

    public bool MagnetUnlocked
    {
        get => magnetUnlocked;
        set => magnetUnlocked = value;
    }

    public bool SolarPanelUnlocked
    {
        get => solarPanelUnlocked;
        set => solarPanelUnlocked = value;
    }

    public int SpeedUpgradeLevel
    {
        get => speedUpgradeLevel;
        set => speedUpgradeLevel = Mathf.Clamp(value, 0, 3);
    }

    public int StorageUpgradeLevel
    {
        get => storageUpgradeLevel;
        set => storageUpgradeLevel = value; // Allow beyond 4 for additional upgrades
    }

    public int EnergyUpgradeLevel
    {
        get => energyUpgradeLevel;
        set => energyUpgradeLevel = Mathf.Clamp(value, 0, 4);
    }

    public int MaxLifeUpgradeLevel
    {
        get => maxLifeUpgradeLevel;
        set => maxLifeUpgradeLevel = Mathf.Clamp(value, 0, 4);
    }

    public int EnginePowerLevel
    {
        get => enginePowerLevel;
        set => enginePowerLevel = Mathf.Clamp(value, 0, 3);
    }
    #endregion

    
    [Header("Regeneration Rates")]
    [SerializeField] private float hpRegenRate = 5f; // HP per second
    [SerializeField] private float energyRegenRate = 1f; // Energy per second
    
    [Header("Emergency States")]
    [SerializeField] private bool emergencyReservesPower = false;
    [SerializeField] private bool emergencyReservesHealth = false;
    public bool EmergencyReservesPowerActive => emergencyReservesPower;
    public bool EmergencyReservesHealthActive => emergencyReservesHealth;

    public float HpRegenRate => hpRegenRate;
    public float EnergyRegenRate => energyRegenRate;
    public void SaveCurrentStats()
    {
        PlayerPrefs.SetInt("CurrentTrash", currentTrash);
        PlayerPrefs.SetInt("TrashNetUnlocked", trashNetUnlocked ? 1 : 0);
        PlayerPrefs.SetInt("IceBreakerUnlocked", iceBreakerUnlocked ? 1 : 0);
        PlayerPrefs.SetInt("SpeedBurstUnlocked", speedBurstUnlocked ? 1 : 0);
        PlayerPrefs.Save();
        Debug.Log("Player stats saved to PlayerPrefs.");
    }

    public void LoadSavedStats()
    {
            playerSpeed = maxPlayerSpeed;

            //currentTrash = PlayerPrefs.GetInt("CurrentTrash", 0); Use this for the trash drop
            trashNetUnlocked = PlayerPrefs.GetInt("TrashNetUnlocked", 0) == 1;
            iceBreakerUnlocked = PlayerPrefs.GetInt("IceBreakerUnlocked", 0) == 1;
            speedBurstUnlocked = PlayerPrefs.GetInt("SpeedBurstUnlocked", 0) == 1;

            emergencyReservesPower = false;
            emergencyReservesHealth = false;

            Debug.Log("Player stats loaded from PlayerPrefs.");
    }

    public void EmergencyReservesPower()
    {
        if (!emergencyReservesPower)
        {
            SaveCurrentStats();
            emergencyReservesPower = true;
            playerSpeed = 2;
            ExpelTrash();
            DisableSpecialAbilities();
            Debug.Log("Emergency Reserves Power activated: HP set to 5, Speed set to 2, Trash expelled, Special abilities disabled.");
        }
    }

    public void EmergencyReservesHealth()
    {
        if (!emergencyReservesHealth)
        {
            SaveCurrentStats();
            emergencyReservesHealth = true;
            playerHP = 5;
            playerSpeed = 2;
            ExpelTrash();
            DisableSpecialAbilities();
            Debug.Log("Emergency Reserves Health activated: HP set to 5, Speed set to 2, Trash expelled, Special abilities disabled.");
        }
    }

    private void ExpelTrash()
    {

    }

    private void DisableSpecialAbilities()
    {
        trashNetUnlocked = false;
        iceBreakerUnlocked = false;
        speedBurstUnlocked = false;
    }

    public void ResetData()
    {
        //recyclePoints = 0;
        playerHP = 100;
        playerEnergy = 10;
        maxPlayerStorage = 5;
        maxPlayerHP = 100;
        maxPlayerEnergy = 10;
        maxPlayerSpeed = 5;
        maxPlayerStorage = 5;
        currentTrash = 0;
        trashNetUnlocked = false;
        trashNetLevel = UnlockLevel.Level1;
        iceBreakerUnlocked = false;
        iceBreakerLevel = UnlockLevel.Level1;
        speedBurstUnlocked = false;
        speedBurstLevel = UnlockLevel.Level1;
        emergencyReservesPower = false;
        emergencyReservesHealth = false;
        solarPanelUnlocked = false;
        magnetUnlocked = false;
        enginePowerLevel = 1;
        maxLifeUpgradeLevel = 1;
        speedUpgradeLevel = 1;
        energyUpgradeLevel = 1;
        storageUpgradeLevel = 1;

    }

    void OnEnable()
    {
        ResetData();
        PlayerPrefs.DeleteAll();
    }
}