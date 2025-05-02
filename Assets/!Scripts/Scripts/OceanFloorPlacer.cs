using UnityEngine;
using System.Collections.Generic;

    [System.Serializable]
    public class BatchDefinition
    {
        [Tooltip("Prefabs to randomly choose from for this batch (e.g., trash variants)")]
        public GameObject[] prefabs;
        [Tooltip("Number of batches to place")]
        public int numberOfBatches = 1;
        [Tooltip("Radius around the batch center where items are placed")]
        public float batchRadius = 2f;
        [Tooltip("Minimum spacing between items within the same batch")]
        public float spacingWithinBatch = 1f;
        [Tooltip("Number of items per batch")]
        public int itemsPerBatch = 3;
        [Tooltip("Can these objects be pushed by the player?")]
        public bool isPushable = false;

        [Tooltip("Perlin noise threshold (0-1) above which this batch is placed")]
        public float noiseThreshold = 0.7f;

        [Tooltip("Use custom rotation instead of default plane rotation")]
        public bool useCustomRotation = false;
        [Tooltip("Custom rotation angles (X, Y, Z) in degrees")]
        public Vector3 customRotation = Vector3.zero;
    }

    [System.Serializable]
    public class IndividualItemDefinition
    {
        [Tooltip("Prefab for this individual item (e.g., bottle)")]
        public GameObject prefab;
        [Tooltip("Number of items to place")]
        public int numberToPlace = 1;
        public float noiseThreshold = 0.5f;

        [Tooltip("Use custom rotation instead of default plane rotation")]
        public bool useCustomRotation = false;
        [Tooltip("Custom rotation angles (X, Y, Z) in degrees")]
        public Vector3 customRotation = Vector3.zero;
    }

public class OceanFloorPlacer : MonoBehaviour
{
    [SerializeField, Tooltip("The plane GameObject representing the ocean floor")]
    private GameObject plane;

    [SerializeField, Tooltip("Global minimum spacing between items from different batches or individual items")]
    private float globalSpacing = 2f;

    [SerializeField, Tooltip("Scale factor for Perlin noise frequency")]
    private float noiseScale = 0.1f;

    [SerializeField, Tooltip("Use raycasting to place objects on the plane's surface")]
    private bool useRaycast = true;

    [SerializeField, Tooltip("Show debug rays for attempted placements (red for batches, blue for items)")]
    private bool showDebugRays = true;

    [SerializeField, Tooltip("Batch definitions for grouped items like coral")]
    private List<BatchDefinition> batches = new List<BatchDefinition>();

    [SerializeField, Tooltip("Individual item definitions like starfish")]
    private List<IndividualItemDefinition> individualItems = new List<IndividualItemDefinition>();

    private Vector3 planeCenter;
    private Vector3 planeExtents;
    private float planeY;

    private const int MAX_ATTEMPTS = 100;

    void Start()
    {
        CalculatePlaneBounds();
        PlaceAllItems();
    }

    // Calculate the world bounds of the plane
    private void CalculatePlaneBounds()
    {
        if (plane == null)
        {
            Debug.LogError("Plane reference is not set in the Inspector!");
            return;
        }

        Mesh mesh = plane.GetComponent<MeshFilter>().sharedMesh;
        Bounds localBounds = mesh.bounds;
        planeCenter = plane.transform.TransformPoint(localBounds.center);
        planeExtents = (plane.transform.TransformPoint(localBounds.max) - plane.transform.TransformPoint(localBounds.min)) * 0.5f;
        planeY = planeCenter.y;

        Debug.Log($"Plane bounds: Center={planeCenter}, Extents={planeExtents}, Y={planeY}");
    }

    // Main function to place all batches and individual items
    private void PlaceAllItems()
    {
        // Place batches first
        foreach (BatchDefinition batch in batches)
        {
            for (int i = 0; i < batch.numberOfBatches; i++)
            {
                if (!PlaceBatch(batch))
                    ForcePlaceBatch(batch);
            }
        }

        // Then place individual items
        foreach (IndividualItemDefinition item in individualItems)
        {
            for (int i = 0; i < item.numberToPlace; i++)
            {
                if (!PlaceIndividualItem(item))
                    ForcePlaceIndividualItem(item);
            }
        }
    }

    // Attempt to place a batch of items
    private bool PlaceBatch(BatchDefinition batch)
    {
        Quaternion spawnRotation = plane.transform.rotation * Quaternion.Euler(-90, 0, 0);

        for (int attempt = 0; attempt < MAX_ATTEMPTS; attempt++)
        {
            Vector3 center = GenerateRandomPosition();
            float noiseValue = Mathf.PerlinNoise(center.x * noiseScale, center.z * noiseScale);

            if (showDebugRays)
                Debug.DrawRay(center, plane.transform.up * 5, Color.red, 5f);

            if (noiseValue < batch.noiseThreshold)
                continue;

            // Check if the batch area is free from other objects
            Collider[] overlaps = Physics.OverlapSphere(center, batch.batchRadius + globalSpacing);
            if (overlaps.Length > 0)
                continue;

            // Generate positions for items within the batch
            List<Vector3> batchPositions = GenerateBatchPositions(center, batch);
            if (batchPositions == null)
                continue;

            // Place all items in the batch
            foreach (Vector3 pos in batchPositions)
            {
                GameObject prefab = batch.prefabs[Random.Range(0, batch.prefabs.Length)];
                if (prefab.GetComponent<Collider>() == null)
                    Debug.LogWarning($"Prefab {prefab.name} has no collider!");
                GameObject spawnedObject = Instantiate(prefab, pos, spawnRotation); // Parentless
                spawnedObject.transform.localScale = prefab.transform.localScale; // Preserve prefab scale
            }

            return true; // Successfully placed the batch
        }

        Debug.LogWarning($"Failed to place batch after {MAX_ATTEMPTS} attempts.");
        return false;
    }

    // Force place a batch (ignore noise and overlap)
    private void ForcePlaceBatch(BatchDefinition batch)
    {
        Quaternion spawnRotation = plane.transform.rotation * Quaternion.Euler(-90, 0, 0);
        Vector3 center = GenerateRandomPosition();
        List<Vector3> batchPositions = GenerateBatchPositions(center, batch);

        if (batchPositions == null)
        {
            Debug.LogWarning("Force placement failed: Unable to generate batch positions.");
            return;
        }

        foreach (Vector3 pos in batchPositions)
        {
            GameObject prefab = batch.prefabs[Random.Range(0, batch.prefabs.Length)];
            GameObject spawnedObject = Instantiate(prefab, pos, spawnRotation); // Parentless
            spawnedObject.transform.localScale = prefab.transform.localScale; // Preserve prefab scale
        }
    }

    // Generate positions for items within a batch, ensuring spacingWithinBatch
    private List<Vector3> GenerateBatchPositions(Vector3 center, BatchDefinition batch)
    {
        List<Vector3> positions = new List<Vector3>();

        for (int i = 0; i < batch.itemsPerBatch; i++)
        {
            for (int attempt = 0; attempt < MAX_ATTEMPTS; attempt++)
            {
                Vector2 offset = Random.insideUnitCircle * batch.batchRadius;
                Vector3 candidate = center + new Vector3(offset.x, 0, offset.y);

                // Adjust Y using raycast or fallback
                if (useRaycast)
                {
                    Vector3 rayStart = candidate + plane.transform.up * 10f;
                    Ray ray = new Ray(rayStart, plane.transform.rotation * Vector3.down);
                    if (Physics.Raycast(ray, out RaycastHit hit, 20f))
                    {
                        if (hit.collider.gameObject == plane)
                        {
                            candidate = hit.point;
                        }
                    }
                }
                else
                {
                    candidate.y = planeY;
                }

                bool valid = true;
                foreach (Vector3 pos in positions)
                {
                    if (Vector3.Distance(candidate, pos) < batch.spacingWithinBatch)
                    {
                        valid = false;
                        break;
                    }
                }

                if (valid)
                {
                    positions.Add(candidate);
                    break;
                }

                if (attempt == MAX_ATTEMPTS - 1)
                    return null; // Failed to place all items
            }
        }

        return positions;
    }

    // Attempt to place an individual item
    private bool PlaceIndividualItem(IndividualItemDefinition item)
    {
        Quaternion spawnRotation = plane.transform.rotation * Quaternion.Euler(-90, 0, 0);

        for (int attempt = 0; attempt < MAX_ATTEMPTS; attempt++)
        {
            Vector3 position = GenerateRandomPosition();
            float noiseValue = Mathf.PerlinNoise(position.x * noiseScale, position.z * noiseScale);

            if (showDebugRays)
                Debug.DrawRay(position, plane.transform.up * 5, Color.blue, 5f);

            if (noiseValue < item.noiseThreshold)
                continue;

            // Check if the position is free from other objects
            Collider[] overlaps = Physics.OverlapSphere(position, globalSpacing / 2f);
            if (overlaps.Length > 0)
                continue;

            if (item.prefab.GetComponent<Collider>() == null)
                Debug.LogWarning($"Prefab {item.prefab.name} has no collider!");
            GameObject spawnedObject = Instantiate(item.prefab, position, spawnRotation); // Parentless
            spawnedObject.transform.localScale = item.prefab.transform.localScale; // Preserve prefab scale
            return true; // Successfully placed the item
        }

        return false;
    }

    // Force place an individual item (ignore noise and overlap)
    private void ForcePlaceIndividualItem(IndividualItemDefinition item)
    {
        Quaternion spawnRotation = plane.transform.rotation * Quaternion.Euler(-90, 0, 0);
        Vector3 position = GenerateRandomPosition();
        GameObject spawnedObject = Instantiate(item.prefab, position, spawnRotation); // Parentless
        spawnedObject.transform.localScale = item.prefab.transform.localScale; // Preserve prefab scale
    }

    // Generate a random position within the plane's bounds
    private Vector3 GenerateRandomPosition()
    {
        float x = Random.Range(planeCenter.x - planeExtents.x, planeCenter.x + planeExtents.x);
        float z = Random.Range(planeCenter.z - planeExtents.z, planeCenter.z + planeExtents.z);

        if (useRaycast)
        {
            Vector3 candidate = new Vector3(x, planeCenter.y + 10f, z);
            Ray ray = new Ray(candidate, plane.transform.rotation * Vector3.down);
            if (Physics.Raycast(ray, out RaycastHit hit, 20f))
            {
                if (hit.collider.gameObject == plane)
                    return hit.point;
            }
        }

        return new Vector3(x, planeY, z); // Fallback
    }
}