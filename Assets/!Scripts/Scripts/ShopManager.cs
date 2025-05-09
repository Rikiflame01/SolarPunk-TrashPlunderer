using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System;
using System.Collections.Generic;

public class ShopManager : MonoBehaviour
{
    [SerializeField] private PlayerData playerData;
    [SerializeField] private Button recycleButton;
    [SerializeField] private Button speedButton;
    [SerializeField] private Button enginePowerButton;
    [SerializeField] private Button storageButton;
    [SerializeField] private Button energyButton;
    [SerializeField] private Button maxLifeButton;
    [SerializeField] private Button netButton;
    [SerializeField] private Button magnetButton;
    [SerializeField] private Button solarPanelButton;
    [SerializeField] private Button iceBreakerButton;
    [SerializeField] private TextMeshProUGUI currentPointsText;
    [SerializeField] private ConfirmationPrompt confirmationPrompt;

    #region Upgrade Data
    private int[] speedUpgradeCosts = { 50, 75, 150 };
    private int[] speedUpgradeEffects = { 2, 4, 4 };
    private int[] enginePowerUpgradeCosts = { 100, 200, 300 };
    private int[] energyUpgradeCosts = { 50, 100, 150, 200 };
    private int[] energyUpgradeEffects = { 5, 10, 15, 20 };
    private int[] maxLifeUpgradeCosts = { 50, 75, 100, 150 };
    private int[] maxLifeUpgradeEffects = { 5, 10, 15, 20 };
    private int[] storageUpgradeCosts = { 25, 50, 75, 100 };
    private int[] storageUpgradeEffects = { 5, 10, 15, 20 };
    private const int ADDITIONAL_STORAGE_COST = 100;
    private const int ADDITIONAL_STORAGE_EFFECT = 10;
    private readonly Dictionary<string, int> specialUnlockCosts = new Dictionary<string, int>
    {
        { "net", 200 },
        { "magnet", 300 },
        { "solarPanel", 400 },
        { "iceBreaker", 500 }
    };
    #endregion

    private void Start()
    {
        // Assign button listeners
        recycleButton.onClick.AddListener(OnRecycleButtonPressed);
        speedButton.onClick.AddListener(OnSpeedButtonPressed);
        enginePowerButton.onClick.AddListener(OnEnginePowerButtonPressed);
        storageButton.onClick.AddListener(OnStorageButtonPressed);
        energyButton.onClick.AddListener(OnEnergyButtonPressed);
        maxLifeButton.onClick.AddListener(OnMaxLifeButtonPressed);
        netButton.onClick.AddListener(() => OnSpecialButtonPressed("net"));
        magnetButton.onClick.AddListener(() => OnSpecialButtonPressed("magnet"));
        solarPanelButton.onClick.AddListener(() => OnSpecialButtonPressed("solarPanel"));
        iceBreakerButton.onClick.AddListener(() => OnSpecialButtonPressed("iceBreaker"));

        UpdateUI();
    }

    private void Update()
    {
        UpdateButtonTransparency();
    }

    #region UI Management
    private void UpdateUI()
    {
        currentPointsText.text = $"{playerData.RecyclePoints}";
    }

    private void UpdateButtonTransparency()
    {
        SetButtonTransparency(speedButton, CanAffordNextSpeedUpgrade());
        SetButtonTransparency(enginePowerButton, CanAffordNextEnginePowerUpgrade());
        SetButtonTransparency(storageButton, CanAffordNextStorageUpgrade());
        SetButtonTransparency(energyButton, CanAffordNextEnergyUpgrade());
        SetButtonTransparency(maxLifeButton, CanAffordNextMaxLifeUpgrade());
        SetButtonTransparency(netButton, CanAffordSpecial("net") && !playerData.TrashNetUnlocked);
        SetButtonTransparency(magnetButton, CanAffordSpecial("magnet") && !playerData.MagnetUnlocked);
        SetButtonTransparency(solarPanelButton, CanAffordSpecial("solarPanel") && !playerData.SolarPanelUnlocked);
        SetButtonTransparency(iceBreakerButton, CanAffordSpecial("iceBreaker") && !playerData.IceBreakerUnlocked);
    }

    private void SetButtonTransparency(Button button, bool canAfford)
    {
        var buttonChild = button.transform.Find("Button");
        if (buttonChild != null)
        {
            var image = buttonChild.GetComponent<Image>();
            if (image != null)
            {
                Color color = image.color;
                color.a = canAfford ? 1f : 0.5f; // 50% transparency if unaffordable
                image.color = color;
            }
            else
            {
                Debug.LogWarning($"No Image component found on 'Button' child of {button.name}");
            }
        }
        else
        {
            Debug.LogWarning($"No child named 'Button' found under {button.name}");
        }
    }

    private IEnumerator MakeButtonRed(Button button)
    {
        var buttonChild = button.transform.Find("Button");
        if (buttonChild != null)
        {
            var image = buttonChild.GetComponent<Image>();
            if (image != null)
            {
                Color originalColor = image.color;
                image.color = new Color(1f, 0f, 0f, originalColor.a); // Red, preserve alpha
                yield return new WaitForSeconds(0.5f);
                image.color = originalColor;
                UpdateButtonTransparency(); // Reset transparency after red flash
            }
        }
    }

    private void ShowConfirmationPrompt(string message, Action onConfirm, bool canAfford)
    {
        confirmationPrompt.Show(message, onConfirm, canAfford);
    }
    #endregion

    #region Button Handlers
    public void OnRecycleButtonPressed()
    {
        int trashToConvert = playerData.CurrentTrash;
        if (trashToConvert <= 0) return;

        string message = $"Convert {trashToConvert} trash into {trashToConvert} recycle points?";
        ShowConfirmationPrompt(message, () =>
        {
            playerData.RecyclePoints += trashToConvert;
            playerData.CurrentTrash = 0;
            UpdateUI();
        }, true); // Recycle always affordable if trash > 0
    }

    public void OnSpeedButtonPressed()
    {
        int currentLevel = playerData.SpeedUpgradeLevel;
        if (currentLevel >= 3) return;

        int cost = speedUpgradeCosts[currentLevel];
        bool canAfford = playerData.RecyclePoints >= cost;

        int effect = speedUpgradeEffects[currentLevel];
        string message = $"This will cost {cost} recycle points to upgrade max speed: {playerData.MaxPlayerSpeed} -> {playerData.MaxPlayerSpeed + effect}";
        if (!canAfford)
            message += "\nInsufficient recycle points!";
        if (currentLevel == 2)
            message += " and unlock Speed Burst special";

        ShowConfirmationPrompt(message, () =>
        {
            playerData.RecyclePoints -= cost;
            playerData.MaxPlayerSpeed += effect;
            playerData.SpeedUpgradeLevel++;
            if (currentLevel == 2)
                playerData.SpeedBurstUnlocked = true;
            UpdateUI();
        }, canAfford);

        if (!canAfford)
        {
            StartCoroutine(MakeButtonRed(speedButton));
        }
    }

    public void OnEnginePowerButtonPressed()
    {
        int currentLevel = playerData.EnginePowerLevel;
        if (currentLevel >= 3) return;

        int cost = enginePowerUpgradeCosts[currentLevel];
        bool canAfford = playerData.RecyclePoints >= cost;

        string message = $"This will cost {cost} recycle points to upgrade engine power to level {currentLevel + 1}";
        if (!canAfford)
            message += "\nInsufficient recycle points!";

        ShowConfirmationPrompt(message, () =>
        {
            playerData.RecyclePoints -= cost;
            playerData.EnginePowerLevel++;
            UpdateUI();
        }, canAfford);

        if (!canAfford)
        {
            StartCoroutine(MakeButtonRed(enginePowerButton));
        }
    }

    public void OnStorageButtonPressed()
    {
        int currentLevel = playerData.StorageUpgradeLevel;
        int cost = currentLevel < 4 ? storageUpgradeCosts[currentLevel] : ADDITIONAL_STORAGE_COST;
        int effect = currentLevel < 4 ? storageUpgradeEffects[currentLevel] : ADDITIONAL_STORAGE_EFFECT;
        bool canAfford = playerData.RecyclePoints >= cost;

        string message = $"This will cost {cost} recycle points to upgrade storage: {playerData.MaxPlayerStorage} -> {playerData.MaxPlayerStorage + effect}";
        if (!canAfford)
            message += "\nInsufficient recycle points!";

        ShowConfirmationPrompt(message, () =>
        {
            playerData.RecyclePoints -= cost;
            playerData.MaxPlayerStorage += effect;
            if (currentLevel < 4)
                playerData.StorageUpgradeLevel++;
            UpdateUI();
        }, canAfford);

        if (!canAfford)
        {
            StartCoroutine(MakeButtonRed(storageButton));
        }
    }

    public void OnEnergyButtonPressed()
    {
        int currentLevel = playerData.EnergyUpgradeLevel;
        if (currentLevel >= 4) return;

        int cost = energyUpgradeCosts[currentLevel];
        bool canAfford = playerData.RecyclePoints >= cost;

        int effect = energyUpgradeEffects[currentLevel];
        string message = $"This will cost {cost} recycle points to upgrade max energy: {playerData.MaxPlayerEnergy} -> {playerData.MaxPlayerEnergy + effect}";
        if (!canAfford)
            message += "\nInsufficient recycle points!";

        ShowConfirmationPrompt(message, () =>
        {
            playerData.RecyclePoints -= cost;
            playerData.MaxPlayerEnergy += effect;
            playerData.EnergyUpgradeLevel++;
            UpdateUI();
        }, canAfford);

        if (!canAfford)
        {
            StartCoroutine(MakeButtonRed(energyButton));
        }
    }

    public void OnMaxLifeButtonPressed()
    {
        int currentLevel = playerData.MaxLifeUpgradeLevel;
        if (currentLevel >= 4) return;

        int cost = maxLifeUpgradeCosts[currentLevel];
        bool canAfford = playerData.RecyclePoints >= cost;

        int effect = maxLifeUpgradeEffects[currentLevel];
        string message = $"This will cost {cost} recycle points to upgrade max HP: {playerData.MaxPlayerHP} -> {playerData.MaxPlayerHP + effect}";
        if (!canAfford)
            message += "\nInsufficient recycle points!";

        ShowConfirmationPrompt(message, () =>
        {
            playerData.RecyclePoints -= cost;
            playerData.MaxPlayerHP += effect;
            playerData.MaxLifeUpgradeLevel++;
            UpdateUI();
        }, canAfford);

        if (!canAfford)
        {
            StartCoroutine(MakeButtonRed(maxLifeButton));
        }
    }

    public void OnSpecialButtonPressed(string specialName)
    {
        bool isUnlocked;
        switch (specialName)
        {
            case "net": isUnlocked = playerData.TrashNetUnlocked; break;
            case "magnet": isUnlocked = playerData.MagnetUnlocked; break;
            case "solarPanel": isUnlocked = playerData.SolarPanelUnlocked; break;
            case "iceBreaker": isUnlocked = playerData.IceBreakerUnlocked; break;
            default: return;
        }

        if (isUnlocked) return;

        int cost = specialUnlockCosts[specialName];
        bool canAfford = playerData.RecyclePoints >= cost;

        string message = $"This will cost {cost} recycle points to unlock {specialName}";
        if (!canAfford)
            message += "\nInsufficient recycle points!";

        ShowConfirmationPrompt(message, () =>
        {
            playerData.RecyclePoints -= cost;
            switch (specialName)
            {
                case "net": playerData.TrashNetUnlocked = true; break;
                case "magnet": playerData.MagnetUnlocked = true; break;
                case "solarPanel": playerData.SolarPanelUnlocked = true; break;
                case "iceBreaker": playerData.IceBreakerUnlocked = true; break;
            }
            UpdateUI();
        }, canAfford);

        if (!canAfford)
        {
            StartCoroutine(MakeButtonRed(GetButtonByName(specialName)));
        }
    }
    #endregion

    #region Helper Methods
    private Button GetButtonByName(string name)
    {
        return name switch
        {
            "net" => netButton,
            "magnet" => magnetButton,
            "solarPanel" => solarPanelButton,
            "iceBreaker" => iceBreakerButton,
            _ => null
        };
    }

    private bool CanAffordNextSpeedUpgrade()
    {
        int level = playerData.SpeedUpgradeLevel;
        return level < 3 && playerData.RecyclePoints >= speedUpgradeCosts[level];
    }

    private bool CanAffordNextEnginePowerUpgrade()
    {
        int level = playerData.EnginePowerLevel;
        return level < 3 && playerData.RecyclePoints >= enginePowerUpgradeCosts[level];
    }

    private bool CanAffordNextStorageUpgrade()
    {
        int level = playerData.StorageUpgradeLevel;
        int cost = level < 4 ? storageUpgradeCosts[level] : ADDITIONAL_STORAGE_COST;
        return playerData.RecyclePoints >= cost;
    }

    private bool CanAffordNextEnergyUpgrade()
    {
        int level = playerData.EnergyUpgradeLevel;
        return level < 4 && playerData.RecyclePoints >= energyUpgradeCosts[level];
    }

    private bool CanAffordNextMaxLifeUpgrade()
    {
        int level = playerData.MaxLifeUpgradeLevel;
        return level < 4 && playerData.RecyclePoints >= maxLifeUpgradeCosts[level];
    }

    private bool CanAffordSpecial(string specialName)
    {
        return playerData.RecyclePoints >= specialUnlockCosts[specialName];
    }
    #endregion
}