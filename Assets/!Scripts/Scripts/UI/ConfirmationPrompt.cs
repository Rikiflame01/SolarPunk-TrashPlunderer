using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class ConfirmationPrompt : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;

    private Action onConfirm;

    public void Show(string message, Action onConfirmAction, bool canAfford)
    {
        gameObject.SetActive(true);
        messageText.text = message;
        onConfirm = onConfirmAction;

        // Show or hide the Yes button based on affordability
        yesButton.gameObject.SetActive(canAfford);

        yesButton.onClick.RemoveAllListeners();
        noButton.onClick.RemoveAllListeners();
        if (canAfford)
            yesButton.onClick.AddListener(OnYesClicked);
        noButton.onClick.AddListener(OnNoClicked);
    }

    private void OnYesClicked()
    {
        onConfirm?.Invoke();
        gameObject.SetActive(false);
    }

    private void OnNoClicked()
    {
        gameObject.SetActive(false);
    }
}