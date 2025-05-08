using UnityEngine;
using System;
using System.Collections.Generic;

public class ShopManager : MonoBehaviour
{
    [Serializable]
    public struct StatPrice
    {
        public string statName; // e.g., "hp", "energy", "speed", "storage"
        public int basePrice; // Initial cost in currentTrash
        public float priceMultiplier; // Multiplier after each purchase (e.g., 2 for 2x)
    }

    [Serializable]
    public struct SpecialPrice
    {
        public string specialName; // e.g., "trashnet", "icebreaker", "speedburst"
        public int unlockBasePrice; // Initial cost to unlock
        public int upgradeBasePrice; // Initial cost to upgrade
        public float unlockPriceMultiplier; // Multiplier after unlocking
        public float upgradePriceMultiplier; // Multiplier after each upgrade
    }

    [SerializeField, Tooltip("PlayerData ScriptableObject to modify")]
    private PlayerData playerData;

    [SerializeField, Tooltip("Pricing for stat increases/decreases")]
    private StatPrice[] statPrices = new StatPrice[]
    {
        new StatPrice { statName = "hp", basePrice = 10, priceMultiplier = 2f },
        new StatPrice { statName = "energy", basePrice = 15, priceMultiplier = 2f },
        new StatPrice { statName = "speed", basePrice = 20, priceMultiplier = 1.5f },
        new StatPrice { statName = "storage", basePrice = 25, priceMultiplier = 2f }
    };

    [SerializeField, Tooltip("Pricing for special ability unlocks and upgrades")]
    private SpecialPrice[] specialPrices = new SpecialPrice[]
    {
        new SpecialPrice { specialName = "trashnet", unlockBasePrice = 50, upgradeBasePrice = 20, unlockPriceMultiplier = 2f, upgradePriceMultiplier = 2f },
        new SpecialPrice { specialName = "icebreaker", unlockBasePrice = 60, upgradeBasePrice = 25, unlockPriceMultiplier = 2f, upgradePriceMultiplier = 2f },
        new SpecialPrice { specialName = "speedburst", unlockBasePrice = 70, upgradeBasePrice = 30, unlockPriceMultiplier = 2f, upgradePriceMultiplier = 2f }
    };

    // Track current prices (updated after purchases)
    private Dictionary<string, int> currentStatPrices = new Dictionary<string, int>();
    private Dictionary<string, int> currentUnlockPrices = new Dictionary<string, int>();
    private Dictionary<string, int> currentUpgradePrices = new Dictionary<string, int>();

    private void Awake()
    {
        // Initialize current prices from base prices
        foreach (var statPrice in statPrices)
        {
            currentStatPrices[statPrice.statName.ToLower()] = statPrice.basePrice;
        }

        foreach (var specialPrice in specialPrices)
        {
            currentUnlockPrices[specialPrice.specialName.ToLower()] = specialPrice.unlockBasePrice;
            currentUpgradePrices[specialPrice.specialName.ToLower()] = specialPrice.upgradeBasePrice;
        }

        // Subscribe to game state changes
        GameManager.OnGameStateChanged += OnGameStateChanged;
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        GameManager.OnGameStateChanged -= OnGameStateChanged;
    }

    private void OnGameStateChanged(GameState newState)
    {
        // Enable/disable shop functionality based on state
        enabled = newState == GameState.Shop;
    }

    // Increase a stat (HP, Energy, Speed, Storage)
    public void IncreaseStat(string statName)
    {
        statName = statName.ToLower();
        if (!currentStatPrices.TryGetValue(statName, out int cost))
        {
            Debug.LogWarning($"Stat {statName} not found in pricing!");
            return;
        }

        if (!CanAfford(cost)) return;

        switch (statName)
        {
            case "hp":
                playerData.PlayerHP += 10;
                break;
            case "energy":
                playerData.PlayerEnergy += 5;
                break;
            case "speed":
                playerData.PlayerSpeed += 1;
                break;
            case "storage":
                playerData.MaxPlayerStorage += 5;
                break;
            default:
                Debug.LogWarning($"Invalid stat name: {statName}");
                return;
        }

        playerData.CurrentTrash -= cost;
        UpdateStatPrice(statName);
        Debug.Log($"Increased {statName} for {cost} trash. New value: {GetStatValue(statName)}, New trash: {playerData.CurrentTrash}, New price: {currentStatPrices[statName]}");
    }

    // Unlock a special ability
    public void UnlockSpecial(string specialName)
    {
        specialName = specialName.ToLower();
        if (!currentUnlockPrices.TryGetValue(specialName, out int cost))
        {
            Debug.LogWarning($"Special {specialName} not found in pricing!");
            return;
        }

        if (!CanAfford(cost)) return;

        switch (specialName)
        {
            case "trashnet":
                if (!playerData.TrashNetUnlocked)
                {
                    playerData.TrashNetUnlocked = true;
                    playerData.TrashNetLevel = UnlockLevel.Level1;
                    playerData.CurrentTrash -= cost;
                    UpdateUnlockPrice(specialName);
                    Debug.Log($"Unlocked TrashNet for {cost} trash. New trash: {playerData.CurrentTrash}, New unlock price: {currentUnlockPrices[specialName]}");
                }
                break;
            case "icebreaker":
                if (!playerData.IceBreakerUnlocked)
                {
                    playerData.IceBreakerUnlocked = true;
                    playerData.IceBreakerLevel = UnlockLevel.Level1;
                    playerData.CurrentTrash -= cost;
                    UpdateUnlockPrice(specialName);
                    Debug.Log($"Unlocked IceBreaker for {cost} trash. New trash: {playerData.CurrentTrash}, New unlock price: {currentUnlockPrices[specialName]}");
                }
                break;
            case "speedburst":
                if (!playerData.SpeedBurstUnlocked)
                {
                    playerData.SpeedBurstUnlocked = true;
                    playerData.SpeedBurstLevel = UnlockLevel.Level1;
                    playerData.CurrentTrash -= cost;
                    UpdateUnlockPrice(specialName);
                    Debug.Log($"Unlocked SpeedBurst for {cost} trash. New trash: {playerData.CurrentTrash}, New unlock price: {currentUnlockPrices[specialName]}");
                }
                break;
            default:
                Debug.LogWarning($"Invalid special name: {specialName}");
                return;
        }
    }

    // Upgrade a special ability
    public void UpgradeSpecial(string specialName)
    {
        specialName = specialName.ToLower();
        if (!currentUpgradePrices.TryGetValue(specialName, out int cost))
        {
            Debug.LogWarning($"Special {specialName} not found in pricing!");
            return;
        }

        if (!CanAfford(cost)) return;

        switch (specialName)
        {
            case "trashnet":
                if (playerData.TrashNetUnlocked && playerData.TrashNetLevel < UnlockLevel.Level3)
                {
                    playerData.TrashNetLevel = playerData.TrashNetLevel + 1;
                    playerData.CurrentTrash -= cost;
                    UpdateUpgradePrice(specialName);
                    Debug.Log($"Upgraded TrashNet to {playerData.TrashNetLevel} for {cost} trash. New trash: {playerData.CurrentTrash}, New upgrade price: {currentUpgradePrices[specialName]}");
                }
                break;
            case "icebreaker":
                if (playerData.IceBreakerUnlocked && playerData.IceBreakerLevel < UnlockLevel.Level3)
                {
                    playerData.IceBreakerLevel = playerData.IceBreakerLevel + 1;
                    playerData.CurrentTrash -= cost;
                    UpdateUpgradePrice(specialName);
                    Debug.Log($"Upgraded IceBreaker to {playerData.IceBreakerLevel} for {cost} trash. New trash: {playerData.CurrentTrash}, New upgrade price: {currentUpgradePrices[specialName]}");
                }
                break;
            case "speedburst":
                if (playerData.SpeedBurstUnlocked && playerData.SpeedBurstLevel < UnlockLevel.Level3)
                {
                    playerData.SpeedBurstLevel = playerData.SpeedBurstLevel + 1;
                    playerData.CurrentTrash -= cost;
                    UpdateUpgradePrice(specialName);
                    Debug.Log($"Upgraded SpeedBurst to {playerData.SpeedBurstLevel} for {cost} trash. New trash: {playerData.CurrentTrash}, New upgrade price: {currentUpgradePrices[specialName]}");
                }
                break;
            default:
                Debug.LogWarning($"Invalid special name: {specialName}");
                return;
        }
    }

    private bool CanAfford(int cost)
    {
        if (playerData.CurrentTrash >= cost)
        {
            return true;
        }
        Debug.LogWarning($"Not enough trash to afford {cost}. Current trash: {playerData.CurrentTrash}");
        return false;
    }

    private void UpdateStatPrice(string statName)
    {
        statName = statName.ToLower();
        foreach (var price in statPrices)
        {
            if (price.statName.ToLower() == statName)
            {
                currentStatPrices[statName] = Mathf.CeilToInt(currentStatPrices[statName] * price.priceMultiplier);
                break;
            }
        }
    }

    private void UpdateUnlockPrice(string specialName)
    {
        specialName = specialName.ToLower();
        foreach (var price in specialPrices)
        {
            if (price.specialName.ToLower() == specialName)
            {
                currentUnlockPrices[specialName] = Mathf.CeilToInt(currentUnlockPrices[specialName] * price.unlockPriceMultiplier);
                break;
            }
        }
    }

    private void UpdateUpgradePrice(string specialName)
    {
        specialName = specialName.ToLower();
        foreach (var price in specialPrices)
        {
            if (price.specialName.ToLower() == specialName)
            {
                currentUpgradePrices[specialName] = Mathf.CeilToInt(currentUpgradePrices[specialName] * price.upgradePriceMultiplier);
                break;
            }
        }
    }

    // Helper to get current stat value for logging
    private int GetStatValue(string statName)
    {
        switch (statName.ToLower())
        {
            case "hp": return playerData.PlayerHP;
            case "energy": return playerData.PlayerEnergy;
            case "speed": return playerData.PlayerSpeed;
            case "storage": return playerData.CurrentTrash;
            default: return 0;
        }
    }
}