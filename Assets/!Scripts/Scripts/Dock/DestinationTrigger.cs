using UnityEngine;

public class DestinationTrigger : MonoBehaviour
{
    [SerializeField, Tooltip("Tag of this trigger ('TargetTrigger' or 'ExitTrigger')")]
    private string triggerTag;

    [SerializeField, Tooltip("Reference to the AutoMoveTrigger script")]
    private AutoMoveTrigger autoMoveTrigger;

    private void OnTriggerEnter(Collider other)
    {
        if (autoMoveTrigger != null)
        {
            autoMoveTrigger.OnDestinationTriggerEnter(other, triggerTag);
        }
        else
        {
            Debug.LogWarning("AutoMoveTrigger not assigned in DestinationTrigger!");
        }
    }

    private void OnValidate()
    {
        Collider collider = GetComponent<Collider>();
        if (collider == null)
        {
            Debug.LogError("DestinationTrigger requires a Collider component!");
        }
        else if (!collider.isTrigger)
        {
            Debug.LogWarning("Collider is not set as a trigger. Setting it now.");
            collider.isTrigger = true;
        }

        if (string.IsNullOrEmpty(triggerTag))
        {
            Debug.LogWarning("Trigger Tag not set in DestinationTrigger! Set to 'TargetTrigger' or 'ExitTrigger'.");
        }
    }
}