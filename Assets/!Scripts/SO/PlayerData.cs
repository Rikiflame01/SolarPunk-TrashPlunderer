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
    [SerializeField] private int playerStorage = 5;
    [SerializeField] private int maxPlayerStorage = 5;
    [SerializeField] private int currentTrash = 0;

    [Header("Special Unlocks")]
    [SerializeField] private bool trashNetUnlocked = false;
    [SerializeField] private UnlockLevel trashNetLevel = UnlockLevel.Level1;
    [SerializeField] private bool iceBreakerUnlocked = false;
    [SerializeField] private UnlockLevel iceBreakerLevel = UnlockLevel.Level1;
    [SerializeField] private bool speedBurstUnlocked = false;
    [SerializeField] private UnlockLevel speedBurstLevel = UnlockLevel.Level1;

    [Header("Emergency States")]
    [SerializeField] private bool emergencyReservesPower = false;
    [SerializeField] private bool emergencyReservesHealth = false;

    [Header("Regeneration Rates")]
    [SerializeField] private float hpRegenRate = 5f; // HP per second
    [SerializeField] private float energyRegenRate = 1f; // Energy per second

    public int PlayerHP
    {
        get => playerHP;
        set => playerHP = Mathf.Clamp(value, 0, maxPlayerHP);
    }

    public int PlayerEnergy
    {
        get => playerEnergy;
        set => playerEnergy = Mathf.Clamp(value, 0, maxPlayerEnergy);
    }

    public float HpRegenRate => hpRegenRate;
    public float EnergyRegenRate => energyRegenRate;

    public int PlayerSpeed
    {
        get => playerSpeed;
        set => playerSpeed = Mathf.Max(0, value);
    }

    public int PlayerStorage
    {
        get => playerStorage;
        set => playerStorage = Mathf.Max(0, value);
    }

    public int CurrentTrash
    {
        get => currentTrash;
        set => currentTrash = Mathf.Max(0, value);
    }

    public bool TrashNetUnlocked
    {
        get => trashNetUnlocked;
        set => trashNetUnlocked = value;
    }

    public UnlockLevel TrashNetLevel
    {
        get => trashNetLevel;
        set => trashNetLevel = value;
    }

    public bool IceBreakerUnlocked
    {
        get => iceBreakerUnlocked;
        set => iceBreakerUnlocked = value;
    }

    public UnlockLevel IceBreakerLevel
    {
        get => iceBreakerLevel;
        set => iceBreakerLevel = value;
    }

    public bool SpeedBurstUnlocked
    {
        get => speedBurstUnlocked;
        set => speedBurstUnlocked = value;
    }

    public UnlockLevel SpeedBurstLevel
    {
        get => speedBurstLevel;
        set => speedBurstLevel = value;
    }

    public bool EmergencyReservesPowerActive => emergencyReservesPower;
    public bool EmergencyReservesHealthActive => emergencyReservesHealth;

    public void SaveCurrentStats()
    {
        PlayerPrefs.SetInt("TrashNetUnlocked", trashNetUnlocked ? 1 : 0);
        PlayerPrefs.SetInt("IceBreakerUnlocked", iceBreakerUnlocked ? 1 : 0);
        PlayerPrefs.SetInt("SpeedBurstUnlocked", speedBurstUnlocked ? 1 : 0);
        PlayerPrefs.Save();
        Debug.Log("Player stats saved to PlayerPrefs.");
    }

    public void LoadSavedStats()
    {
        if (PlayerPrefs.HasKey("PlayerMaxHP"))
        {
            playerHP = maxPlayerHP;
            playerEnergy = maxPlayerEnergy;
            playerSpeed = maxPlayerSpeed;
            playerStorage = maxPlayerStorage;
            trashNetUnlocked = PlayerPrefs.GetInt("TrashNetUnlocked", 0) == 1;
            iceBreakerUnlocked = PlayerPrefs.GetInt("IceBreakerUnlocked", 0) == 1;
            speedBurstUnlocked = PlayerPrefs.GetInt("SpeedBurstUnlocked", 0) == 1;

            emergencyReservesPower = false;
            emergencyReservesHealth = false;

            Debug.Log("Player stats loaded from PlayerPrefs.");
        }
        else
        {
            ResetData();
            Debug.Log("No saved stats found. Reset to default values.");
        }
    }

    public void EmergencyReservesPower()
    {
        if (!emergencyReservesPower)
        {
            emergencyReservesPower = true;
            playerHP = 5;
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
        currentTrash = 0; // Reset current trash
        // TODO: Implement logic to drop a trash crate (e.g., instantiate a GameObject)
    }

    private void DisableSpecialAbilities()
    {
        trashNetUnlocked = false;
        iceBreakerUnlocked = false;
        speedBurstUnlocked = false;
        // Levels are preserved as per requirement
    }

    public void ResetData()
    {
        playerHP = 100;
        playerEnergy = 10;
        playerSpeed = 5;
        playerStorage = 5;
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
    }

    void OnEnable()
    {
        ResetData();
    }
}