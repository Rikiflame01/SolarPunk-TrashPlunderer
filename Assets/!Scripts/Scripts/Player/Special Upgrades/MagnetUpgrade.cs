using UnityEngine;
using System.Collections.Generic;

public class MagnetUpgrade : MonoBehaviour
{
    [SerializeField, Tooltip("Magnet prefab GameObjects to activate on upgrade")]
    private GameObject[] magnetPrefab;

    [SerializeField, Tooltip("Player data ScriptableObject for magnet unlock status")]
    private PlayerData playerData;

    [SerializeField, Tooltip("Radius around the player for attracting trash (meters)")]
    private float attractionRadius = 10f;

    [SerializeField, Tooltip("Minimum distance trash can approach the player (meters)")]
    private float minDistance = 2f;

    [SerializeField, Tooltip("Force applied to attract trash (Newtons)")]
    private float attractionForce = 50f;

    [SerializeField, Tooltip("Maximum number of trash objects to attract at once")]
    private int maxAttractedObjects = 20;

    [SerializeField, Tooltip("Enable debug logs for magnet attraction")]
    private bool debug = false;

    private bool isMagnetActive = false;
    private readonly List<Rigidbody> attractedRigidbodies = new List<Rigidbody>();

    private void OnEnable()
    {
        ActionManager.OnMagnetUpgrade += OnMagnetUpgrade;
    }

    private void OnDisable()
    {
        ActionManager.OnMagnetUpgrade -= OnMagnetUpgrade;
    }

    private void Start()
    {
        // Validate references
        if (playerData == null)
            Debug.LogError("PlayerData not assigned in MagnetUpgrade!");
        if (magnetPrefab == null || magnetPrefab.Length == 0)
            Debug.LogError("MagnetPrefab array not assigned or empty in MagnetUpgrade!");
        if (minDistance >= attractionRadius)
            Debug.LogError($"MinDistance ({minDistance}) must be less than AttractionRadius ({attractionRadius}) in MagnetUpgrade!");

        // Ensure magnet prefabs are initially inactive
        foreach (GameObject magnet in magnetPrefab)
        {
            if (magnet != null)
                magnet.SetActive(false);
            else
                Debug.LogWarning("Null magnet prefab found in MagnetUpgrade!");
        }

        // Check if magnet is already unlocked
        if (playerData != null && playerData.MagnetUnlocked)
        {
            OnMagnetUpgrade();
        }
    }

    private void FixedUpdate()
    {
        if (!isMagnetActive || playerData == null || GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.GamePlay)
            return;

        // Clear previous list to re-evaluate attracted objects
        attractedRigidbodies.Clear();

        // Find trash objects within radius
        Collider[] colliders = Physics.OverlapSphere(transform.position, attractionRadius, LayerMask.GetMask("Trash"));
        int objectsToAttract = Mathf.Min(colliders.Length, maxAttractedObjects);

        for (int i = 0; i < objectsToAttract; i++)
        {
            Collider col = colliders[i];
            if (col.gameObject.layer != LayerMask.NameToLayer("Trash"))
                continue;

            // Verify TrashProperties (optional, for consistency)
            TrashProperties properties = col.GetComponent<TrashProperties>();
            if (properties == null)
            {
                if (debug)
                    Debug.LogWarning($"Trash object '{col.gameObject.name}' has no TrashProperties component. Still attracting.");
            }

            // Get Rigidbody
            Rigidbody rb = col.GetComponent<Rigidbody>();
            if (rb == null)
            {
                if (debug)
                    Debug.LogWarning($"Trash object '{col.gameObject.name}' has no Rigidbody. Skipping attraction.");
                continue;
            }

            // Check distance to player
            float distance = Vector3.Distance(transform.position, rb.position);
            if (distance <= minDistance)
            {
                if (debug)
                    Debug.Log($"Trash '{rb.gameObject.name}' is within minDistance ({distance:F1}m <= {minDistance}m). Skipping attraction.");
                continue;
            }

            attractedRigidbodies.Add(rb);
        }

        // Apply attraction force to each Rigidbody
        foreach (Rigidbody rb in attractedRigidbodies)
        {
            Vector3 direction = (transform.position - rb.position).normalized;
            rb.AddForce(direction * attractionForce, ForceMode.Force);

            if (debug)
                Debug.Log($"Attracting trash '{rb.gameObject.name}' with force {attractionForce}. Distance: {(transform.position - rb.position).magnitude:F1}m");
        }

        if (debug && attractedRigidbodies.Count > 0)
            Debug.Log($"Magnet attracting {attractedRigidbodies.Count} trash objects (max {maxAttractedObjects}).");
    }

    private void OnMagnetUpgrade()
    {
        // Activate magnet prefabs
        foreach (GameObject magnet in magnetPrefab)
        {
            if (magnet != null)
                magnet.SetActive(true);
            else
                Debug.LogWarning("Null magnet prefab found in MagnetUpgrade!");
        }

        // Enable magnet functionality
        isMagnetActive = true;

        if (debug)
            Debug.Log($"Magnet upgrade activated: Attracting up to {maxAttractedObjects} trash objects within {attractionRadius}m radius, stopping at {minDistance}m.");
    }

}