using UnityEngine;
using System;

public class TrashRecycler : MonoBehaviour
{
    [SerializeField, Tooltip("Player data ScriptableObject for recycle points")]
    private PlayerData playerData;

    [SerializeField, Tooltip("Enable debug logs for recycling events")]
    private bool debug = false;

    // Event for UI or other systems to react to recycling
    public static event Action<int> OnTrashRecycled;

    private Collider triggerCollider;

    private void Awake()
    {
        // Validate trigger collider
        triggerCollider = GetComponent<Collider>();
        if (triggerCollider == null || !triggerCollider.isTrigger)
        {
            Debug.LogError("TrashRecycler requires a trigger Collider! Disabling script.");
            enabled = false;
            return;
        }

        // Validate player data
        if (playerData == null)
        {
            Debug.LogError("PlayerData not assigned in TrashRecycler! Disabling script.");
            enabled = false;
            return;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if the object is on the Trash layer
        if (other.gameObject.layer != LayerMask.NameToLayer("Trash"))
        {
            if (debug)
                Debug.Log($"Object '{other.gameObject.name}' entered trigger but is not on Trash layer (Layer: {LayerMask.LayerToName(other.gameObject.layer)}).");
            return;
        }

        // Get TrashProperties component
        TrashProperties properties = other.GetComponent<TrashProperties>();
        if (properties == null)
        {
            Debug.LogWarning($"Trash object '{other.gameObject.name}' has no TrashProperties component! Skipping.");
            return;
        }

        // Recycle the trash
        int points = properties.GetPoints();
        playerData.RecyclePoints += points;

        // Notify listeners (e.g., for UI feedback)
        OnTrashRecycled?.Invoke(points);

        if (debug)
            Debug.Log($"Recycled trash '{other.gameObject.name}' (Class: {properties.TrashClass}) for {points} recycle points. Total RecyclePoints: {playerData.RecyclePoints}");

        // Destroy the trash object
        Destroy(other.gameObject);
    }

}