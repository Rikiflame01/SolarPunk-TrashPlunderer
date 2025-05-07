using UnityEngine;
using UnityEngine.UI;

public class CommunicationManager : MonoBehaviour
{
    [SerializeField, Tooltip("Root GameObject to enable for displaying docking status (e.g., Communication)")]
    private GameObject statusRoot;

    [SerializeField, Tooltip("UI Image on the Status child for displaying docking status")]
    private Image statusImage;

    [SerializeField, Tooltip("Sprite for docking complete")]
    private Sprite completeSprite;

    [SerializeField, Tooltip("Sprite for docking incomplete")]
    private Sprite incompleteSprite;

    [SerializeField, Tooltip("Time to display the status image (seconds)")]
    private float displayDuration = 1f;

    [SerializeField, Tooltip("Enable debug logs")]
    private bool debug = true;

    private float displayTimer;

    private void Start()
    {
        // Ensure root and image are disabled by default
        if (statusRoot != null)
        {
            statusRoot.SetActive(false);
            if (debug)
                Debug.Log($"StatusRoot (Communication) disabled at start: {statusRoot.activeSelf}");
        }
        else
        {
            Debug.LogWarning("StatusRoot (Communication) not assigned in CommunicationManager!");
        }

        if (statusImage != null)
        {
            statusImage.enabled = false;
            if (debug)
                Debug.Log($"StatusImage disabled at start: {statusImage.enabled}");
        }
        else
        {
            Debug.LogWarning("StatusImage not assigned in CommunicationManager!");
        }

        // Subscribe to ActionManager events
        ActionManager.OnDockingComplete += Complete;
        ActionManager.OnDockingIncomplete += Incomplete;
        ActionManager.OnDockingInProgress += InProgress;

        if (debug)
            Debug.Log("CommunicationManager initialized and subscribed to ActionManager events.");
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        ActionManager.OnDockingComplete -= Complete;
        ActionManager.OnDockingIncomplete -= Incomplete;
        ActionManager.OnDockingInProgress -= InProgress;
    }

    private void Update()
    {
        if (displayTimer > 0)
        {
            displayTimer -= Time.deltaTime;
            if (displayTimer <= 0)
            {
                if (statusRoot != null)
                {
                    statusRoot.SetActive(false);
                    if (debug)
                        Debug.Log($"StatusRoot (Communication) disabled: {statusRoot.activeSelf}");
                }
                if (statusImage != null)
                {
                    statusImage.enabled = false;
                    if (debug)
                        Debug.Log($"StatusImage disabled: {statusImage.enabled}");
                }
                if (debug)
                    Debug.Log("Status root and image hidden after display duration.");
            }
        }
    }

    public void Complete()
    {
        if (statusRoot == null || statusImage == null || completeSprite == null)
        {
            Debug.LogWarning($"Complete failed: StatusRoot={(statusRoot != null ? "Assigned" : "Null")}, StatusImage={(statusImage != null ? "Assigned" : "Null")}, CompleteSprite={(completeSprite != null ? "Assigned" : "Null")}");
            return;
        }

        statusRoot.SetActive(true);
        statusImage.sprite = completeSprite;
        statusImage.enabled = true;
        displayTimer = displayDuration;

        if (debug)
            Debug.Log($"Docking Complete: Enabled StatusRoot (Communication) and showing complete sprite. Root active: {statusRoot.activeSelf}, Image enabled: {statusImage.enabled}");
    }

    public void Incomplete()
    {
        if (statusRoot == null || statusImage == null || incompleteSprite == null)
        {
            Debug.LogWarning($"Incomplete failed: StatusRoot={(statusRoot != null ? "Assigned" : "Null")}, StatusImage={(statusImage != null ? "Assigned" : "Null")}, IncompleteSprite={(incompleteSprite != null ? "Assigned" : "Null")}");
            return;
        }

        statusRoot.SetActive(true);
        statusImage.sprite = incompleteSprite;
        statusImage.enabled = true;
        displayTimer = displayDuration;

        if (debug)
            Debug.Log($"Docking Incomplete: Enabled StatusRoot (Communication) and showing incomplete sprite. Root active: {statusRoot.activeSelf}, Image enabled: {statusImage.enabled}");
    }

    public void InProgress()
    {
        // Placeholder for future use (handled by AutoMoveTrigger)
        if (debug)
            Debug.Log("Docking In Progress: No action taken.");
    }
}