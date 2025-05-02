using UnityEngine;

// Enum to define levels for special unlocks
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
    [SerializeField] private int playerEnergy = 10;
    [SerializeField] private int playerSpeed = 5;
    [SerializeField] private int playerStorage = 5;
    [SerializeField] private int currentTrash = 0;

    [Header("Special Unlocks")]
    [SerializeField] private bool trashNetUnlocked = false;
    [SerializeField] private UnlockLevel trashNetLevel = UnlockLevel.Level1;
    [SerializeField] private bool iceBreakerUnlocked = false;
    [SerializeField] private UnlockLevel iceBreakerLevel = UnlockLevel.Level1;
    [SerializeField] private bool speedBurst = false;
    [SerializeField] private UnlockLevel speedBurstLevel = UnlockLevel.Level1;

    public int PlayerHP
    {
        get => playerHP;
        set => playerHP = Mathf.Max(0, value);
    }

    public int PlayerEnergy
    {
        get => playerEnergy;
        set => playerEnergy = Mathf.Max(0, value);
    }

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

    public bool SpeedBurst
    {
        get => speedBurst;
        set => speedBurst = value;
    }

    public UnlockLevel SpeedBurstLevel
    {
        get => speedBurstLevel;
        set => speedBurstLevel = value;
    }

    public void ResetData()
    {
        playerHP = 100;
        playerEnergy = 10;
        playerSpeed = 5;
        playerStorage = 5;
        currentTrash = 0;
        trashNetUnlocked = false;
        trashNetLevel = UnlockLevel.Level1;
        iceBreakerUnlocked = false;
        iceBreakerLevel = UnlockLevel.Level1;
        speedBurst = false;
        speedBurstLevel = UnlockLevel.Level1;
    }
}