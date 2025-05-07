using UnityEngine;
using System.Collections.Generic;namespace TrashSpawner
{
    public class SurfaceTrashSpawner : MonoBehaviour
    {
        [SerializeField, Tooltip("The plane GameObject representing the surface")]
        private GameObject plane;

    [SerializeField, Tooltip("Trigger collider defining the spawn area")]
    private Collider spawnTriggerCollider;

    [SerializeField, Tooltip("Global minimum spacing between items")]
    private float globalSpacing = 1f;

    [SerializeField, Tooltip("Show debug rays for attempted placements")]
    private bool showDebugRays = true;

    [SerializeField, Tooltip("Draw spawn area bounds for debugging")]
    private bool drawSpawnBounds = false;

    [SerializeField, Tooltip("Batch definitions for grouped trash")]
    private List<BatchDefinition> batches = new List<BatchDefinition>();

    [SerializeField, Tooltip("Individual trash items")]
    private List<IndividualItemDefinition> individualItems = new List<IndividualItemDefinition>();

    private Vector3 planeCenter;
    private Vector3 planeExtents;
    private float planeY;
    private Bounds spawnAreaBounds;

    private const int MAX_ATTEMPTS = 100;
    private const float FLOATING_Y = 1.95f;

    void Start()
    {
        CalculatePlaneBounds();
        PlaceAllItems();
        if (spawnTriggerCollider != null)
        {
            spawnTriggerCollider.enabled = false;
            Debug.Log("Spawn trigger collider disabled after spawning.");
        }
    }

    void Update()
    {
        if (drawSpawnBounds && spawnAreaBounds.size != Vector3.zero)
        {
            Vector3 min = spawnAreaBounds.min;
            Vector3 max = spawnAreaBounds.max;
            Debug.DrawLine(new Vector3(min.x, FLOATING_Y, min.z), new Vector3(max.x, FLOATING_Y, min.z), Color.green, 0.1f);
            Debug.DrawLine(new Vector3(max.x, FLOATING_Y, min.z), new Vector3(max.x, FLOATING_Y, max.z), Color.green, 0.1f);
            Debug.DrawLine(new Vector3(max.x, FLOATING_Y, max.z), new Vector3(min.x, FLOATING_Y, max.z), Color.green, 0.1f);
            Debug.DrawLine(new Vector3(min.x, FLOATING_Y, max.z), new Vector3(min.x, FLOATING_Y, min.z), Color.green, 0.1f);
        }

    }

    private void CalculatePlaneBounds()
    {
        if (plane == null || spawnTriggerCollider == null)
        {
            Debug.LogError("Plane or spawn trigger collider not set!");
            return;
        }
        if (!spawnTriggerCollider.isTrigger)
        {
            spawnTriggerCollider.isTrigger = true;
            Debug.LogWarning("Spawn collider set as trigger.");
        }

        Mesh mesh = plane.GetComponent<MeshFilter>().sharedMesh;
        Bounds localBounds = mesh.bounds;
        planeCenter = plane.transform.TransformPoint(localBounds.center);
        planeExtents = (plane.transform.TransformPoint(localBounds.max) - plane.transform.TransformPoint(localBounds.min)) * 0.5f;
        planeY = planeCenter.y;
        spawnAreaBounds = spawnTriggerCollider.bounds;
    }

    private void PlaceAllItems()
    {
        foreach (BatchDefinition batch in batches)
        {
            for (int i = 0; i < batch.numberOfBatches; i++)
            {
                if (!PlaceBatch(batch))
                    ForcePlaceBatch(batch);
            }
        }

        foreach (IndividualItemDefinition item in individualItems)
        {
            for (int i = 0; i < item.numberToPlace; i++)
            {
                if (!PlaceIndividualItem(item))
                    ForcePlaceIndividualItem(item);
            }
        }
    }

    private bool PlaceBatch(BatchDefinition batch)
    {
        Quaternion spawnRotation = batch.useCustomRotation
            ? Quaternion.Euler(batch.customRotation)
            : plane.transform.rotation;

        for (int attempt = 0; attempt < MAX_ATTEMPTS; attempt++)
        {
            Vector3 center = GenerateRandomPosition();
            if (showDebugRays)
                Debug.DrawRay(center, plane.transform.up * 5, Color.red, 5f);

            Collider[] overlaps = Physics.OverlapSphere(center, batch.batchRadius + globalSpacing);
            if (overlaps.Length > 0)
                continue;

            List<Vector3> batchPositions = GenerateBatchPositions(center, batch);
            if (batchPositions == null)
                continue;

            foreach (Vector3 pos in batchPositions)
            {
                GameObject prefab = batch.prefabs[Random.Range(0, batch.prefabs.Length)];
                if (prefab.GetComponent<Collider>() == null)
                    Debug.LogWarning($"Prefab {prefab.name} has no collider!");
                GameObject spawnedObject = Instantiate(prefab, pos, spawnRotation);
                spawnedObject.transform.localScale = prefab.transform.localScale;
                SetupTrashObject(spawnedObject, batch.isPushable);
            }

            return true;
        }

        Debug.LogWarning($"Failed to place batch after {MAX_ATTEMPTS} attempts.");
        return false;
    }

    private void ForcePlaceBatch(BatchDefinition batch)
    {
        Quaternion spawnRotation = batch.useCustomRotation
            ? Quaternion.Euler(batch.customRotation)
            : plane.transform.rotation;

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
            GameObject spawnedObject = Instantiate(prefab, pos, spawnRotation);
            spawnedObject.transform.localScale = prefab.transform.localScale;
            SetupTrashObject(spawnedObject, batch.isPushable);
        }
    }

    private List<Vector3> GenerateBatchPositions(Vector3 center, BatchDefinition batch)
    {
        List<Vector3> positions = new List<Vector3>();

        for (int i = 0; i < batch.itemsPerBatch; i++)
        {
            for (int attempt = 0; attempt < MAX_ATTEMPTS; attempt++)
            {
                Vector2 offset = Random.insideUnitCircle * batch.batchRadius;
                Vector3 candidate = center + new Vector3(offset.x, 0, offset.y);
                candidate.y = FLOATING_Y;

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
                    return null;
            }
        }

        return positions;
    }

    private bool PlaceIndividualItem(IndividualItemDefinition item)
    {
        Quaternion spawnRotation = item.useCustomRotation
            ? Quaternion.Euler(item.customRotation)
            : plane.transform.rotation;

        for (int attempt = 0; attempt < MAX_ATTEMPTS; attempt++)
        {
            Vector3 position = GenerateRandomPosition();
            if (showDebugRays)
                Debug.DrawRay(position, plane.transform.up * 5, Color.blue, 5f);

            Collider[] overlaps = Physics.OverlapSphere(position, globalSpacing / 2f);
            if (overlaps.Length > 0)
                continue;

            if (item.prefab.GetComponent<Collider>() == null)
                Debug.LogWarning($"Prefab {item.prefab.name} has no collider!");
            GameObject spawnedObject = Instantiate(item.prefab, position, spawnRotation);
            spawnedObject.transform.localScale = item.prefab.transform.localScale;
            SetupTrashObject(spawnedObject, false); // Individual items not pushable
            return true;
        }

        Debug.LogWarning($"Failed to place individual item after {MAX_ATTEMPTS} attempts.");
        return false;
    }

    private void ForcePlaceIndividualItem(IndividualItemDefinition item)
    {
        Quaternion spawnRotation = item.useCustomRotation
            ? Quaternion.Euler(item.customRotation)
            : plane.transform.rotation;

        Vector3 position = GenerateRandomPosition();
        GameObject spawnedObject = Instantiate(item.prefab, position, spawnRotation);
        spawnedObject.transform.localScale = item.prefab.transform.localScale;
        SetupTrashObject(spawnedObject, false);
        Debug.Log($"Force placed individual item at {position}");
    }

    private Vector3 GenerateRandomPosition()
    {
        Bounds bounds = spawnAreaBounds;
        for (int attempt = 0; attempt < MAX_ATTEMPTS; attempt++)
        {
            float x = Random.Range(bounds.min.x, bounds.max.x);
            float z = Random.Range(bounds.min.z, bounds.max.z);
            Vector3 candidate = new Vector3(x, FLOATING_Y, z);

            if (bounds.Contains(candidate))
                return candidate;
        }

        Debug.LogWarning("Failed to find valid position within spawn area.");
        return new Vector3(planeCenter.x, FLOATING_Y, planeCenter.z);
    }

    private void SetupTrashObject(GameObject trash, bool isPushable)
    {
        int trashLayer = LayerMask.NameToLayer("Trash");
        if (trashLayer == -1)
        {
            Debug.LogError("Trash layer not found! Assigning Default layer. Please create a 'Trash' layer in Tags and Layers.");
            trashLayer = LayerMask.NameToLayer("Default");
        }
        trash.layer = trashLayer;

        Rigidbody rb = trash.GetComponent<Rigidbody>();
        if (rb == null)
            rb = trash.AddComponent<Rigidbody>();

        if (isPushable)
        {
            rb.isKinematic = false;
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.mass = 1f;
        }
        else
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
        }

        FloatingManager manager = FindFirstObjectByType<FloatingManager>();
        if (manager != null)
            manager.AddFloater(trash, isPushable);
    }
}

public class FloatingManager : MonoBehaviour
{
    private class FloatData
    {
        public Transform transform;
        public float rotationSpeed;
        public float offset;
        public bool isPushable;
    }

    private List<FloatData> floaters = new List<FloatData>();

    public void AddFloater(GameObject trash, bool isPushable)
    {
        FloatData data = new FloatData
        {
            transform = trash.transform,
            rotationSpeed = Random.Range(-10f, 10f),
            offset = Random.Range(0f, 2f * Mathf.PI),
            isPushable = isPushable
        };
        floaters.Add(data);
    }

    void Update()
    {
        for (int i = 0; i < floaters.Count; i++)
        {
            var data = floaters[i];
            // Apply subtle rotation for visual effect
            data.transform.Rotate(0, data.rotationSpeed * Time.deltaTime, 0);
        }
    }
}

}

