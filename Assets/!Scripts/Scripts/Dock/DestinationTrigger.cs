using UnityEngine;

public class DestinationTrigger : MonoBehaviour
{
    [SerializeField, Tooltip("Tag of this trigger ('TargetTrigger' or 'ExitTrigger')")]
    private string triggerTag;

    [SerializeField, Tooltip("Reference to the AutoMoveTrigger script")]
    private AutoMoveTrigger autoMoveTrigger;

    [SerializeField, Tooltip("Player data ScriptableObject for RecyclePoints and TrashNetUnlocked")]
    private PlayerData playerData;

    [SerializeField, Tooltip("Reference to the NetUpgrade script for net storage handling")]
    private NetUpgrade netUpgrade;

    [SerializeField, Tooltip("Enable debug logs for trigger and conversion")]
    private bool debug = true;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            if (debug)
                Debug.Log($"DestinationTrigger ({triggerTag}) ignored: Collider {other.name} is not tagged 'Player'.");
            return;
        }

        if (debug)
            Debug.Log($"DestinationTrigger ({triggerTag}) entered by Player.");

        // Convert net storage to recycle points for TargetTrigger
        if (triggerTag == "TargetTrigger")
        {
            if (playerData == null)
            {
                Debug.LogError($"PlayerData is null in DestinationTrigger ({triggerTag})! Cannot convert net storage.");
            }
            else if (netUpgrade == null)
            {
                if (debug && playerData.TrashNetUnlocked)
                    Debug.LogWarning($"NetUpgrade is null in DestinationTrigger ({triggerTag})! Cannot convert net storage.");
            }
            else if (!playerData.TrashNetUnlocked)
            {
                if (debug)
                    Debug.Log($"TrashNetUnlocked is false in DestinationTrigger ({triggerTag}). Skipping net storage conversion.");
            }
            else
            {
                int points = netUpgrade.GetCurrentNetStorage();
                if (points > 0)
                {
                    playerData.RecyclePoints += points;
                    netUpgrade.ResetNet();
                    if (debug)
                        Debug.Log($"DestinationTrigger ({triggerTag}): Converted {points} net points to recycle points. Total: {playerData.RecyclePoints}");
                }
                else if (debug)
                {
                    Debug.Log($"DestinationTrigger ({triggerTag}): Net storage is 0. No points to convert.");
                }
            }
        }

        // Call AutoMoveTrigger for docking/undocking logic
        if (autoMoveTrigger != null)
        {
            autoMoveTrigger.OnDestinationTriggerEnter(other, triggerTag);
            if (debug)
                Debug.Log($"Called AutoMoveTrigger.OnDestinationTriggerEnter with triggerTag: {triggerTag}");
        }
        else
        {
            Debug.LogWarning($"AutoMoveTrigger not assigned in DestinationTrigger ({triggerTag})!");
        }
    }

    private void OnValidate()
    {
        Collider collider = GetComponent<Collider>();
        if (collider == null)
        {
            Debug.LogError($"DestinationTrigger ({name}) requires a Collider component!");
        }
        else if (!collider.isTrigger)
        {
            Debug.LogWarning($"Collider in DestinationTrigger ({name}) is not set as a trigger. Setting it now.");
            collider.isTrigger = true;
        }

        if (string.IsNullOrEmpty(triggerTag))
        {
            Debug.LogWarning($"Trigger Tag not set in DestinationTrigger ({name})! Set to 'TargetTrigger' or 'ExitTrigger'.");
        }

        if (playerData == null)
        {
            Debug.LogWarning($"PlayerData not assigned in DestinationTrigger ({name})!");
        }

        if (netUpgrade == null)
        {
            Debug.LogWarning($"NetUpgrade not assigned in DestinationTrigger ({name})!");
        }
    }
}