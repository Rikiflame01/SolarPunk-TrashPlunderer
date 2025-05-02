using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Burst;
using System.Collections.Generic;
using UnityEngine.Profiling;

namespace TrashSpawner
{
    public class SurfaceTrashSpawner : MonoBehaviour
    {
        [SerializeField, Tooltip("The plane GameObject representing the surface")]
        private GameObject plane;

        [SerializeField, Tooltip("Trigger collider defining the spawn area")]
        private Collider spawnTriggerCollider;

        [SerializeField, Tooltip("Global minimum spacing between items")]
        private float globalSpacing = 0.3f;

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
        private Transform trashContainer;
        private Dictionary<GameObject, Queue<GameObject>> objectPool;

        private const int MAX_ATTEMPTS = 30;
        private const float FLOATING_Y = 1.95f;
        private const float GRID_CELL_SIZE = 1f;

        void Start()
        {
            GameObject containerObj = new GameObject("TrashContainer");
            trashContainer = containerObj.transform;
            trashContainer.parent = transform;
            objectPool = new Dictionary<GameObject, Queue<GameObject>>();

            CalculatePlaneBounds();
            InitializeObjectPool();
            PlaceAllItemsWithJobs();
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

        private void InitializeObjectPool()
        {
            Profiler.BeginSample("InitializeObjectPool");
            foreach (BatchDefinition batch in batches)
            {
                foreach (GameObject prefab in batch.prefabs)
                {
                    if (!objectPool.ContainsKey(prefab))
                        objectPool[prefab] = new Queue<GameObject>();
                    int count = Mathf.CeilToInt(batch.numberOfBatches * batch.itemsPerBatch * 0.5f);
                    for (int i = 0; i < count; i++)
                    {
                        GameObject obj = Instantiate(prefab, Vector3.zero, Quaternion.identity, trashContainer);
                        obj.SetActive(false);
                        objectPool[prefab].Enqueue(obj);
                    }
                }
            }
            foreach (IndividualItemDefinition item in individualItems)
            {
                if (!objectPool.ContainsKey(item.prefab))
                    objectPool[item.prefab] = new Queue<GameObject>();
                int count = Mathf.CeilToInt(item.numberToPlace * 0.5f);
                for (int i = 0; i < count; i++)
                {
                    GameObject obj = Instantiate(item.prefab, Vector3.zero, Quaternion.identity, trashContainer);
                    obj.SetActive(false);
                    objectPool[item.prefab].Enqueue(obj);
                }
            }
            Profiler.EndSample();
        }

        private GameObject GetPooledObject(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (objectPool.ContainsKey(prefab) && objectPool[prefab].Count > 0)
            {
                GameObject obj = objectPool[prefab].Dequeue();
                obj.transform.SetPositionAndRotation(position, rotation);
                obj.transform.localScale = prefab.transform.localScale;
                obj.SetActive(true);
                return obj;
            }
            GameObject newObj = Instantiate(prefab, position, rotation, trashContainer);
            newObj.transform.localScale = prefab.transform.localScale;
            if (!objectPool.ContainsKey(prefab))
                objectPool[prefab] = new Queue<GameObject>();
            return newObj;
        }

        private void PlaceAllItemsWithJobs()
        {
            Profiler.BeginSample("PlaceAllItemsWithJobs");
            NativeList<Vector3> allPositions = new NativeList<Vector3>(Allocator.TempJob);
            List<(Vector3 position, GameObject prefab, bool isPushable, Quaternion rotation)> spawnData = new List<(Vector3, GameObject, bool, Quaternion)>();
            NativeList<JobHandle> jobHandles = new NativeList<JobHandle>(Allocator.TempJob);

            int gridWidth = Mathf.CeilToInt((spawnAreaBounds.max.x - spawnAreaBounds.min.x) / GRID_CELL_SIZE);
            int gridHeight = Mathf.CeilToInt((spawnAreaBounds.max.z - spawnAreaBounds.min.z) / GRID_CELL_SIZE);
            NativeArray<int> gridCounts = new NativeArray<int>(gridWidth * gridHeight, Allocator.TempJob);

            foreach (BatchDefinition batch in batches)
            {
                Quaternion spawnRotation = batch.useCustomRotation
                    ? Quaternion.Euler(batch.customRotation)
                    : plane.transform.rotation;

                for (int i = 0; i < batch.numberOfBatches; i++)
                {
                    NativeArray<Vector3> batchPositions = new NativeArray<Vector3>(batch.itemsPerBatch, Allocator.TempJob);
                    PlaceBatchJob batchJob = new PlaceBatchJob
                    {
                        BoundsMin = spawnAreaBounds.min,
                        BoundsMax = spawnAreaBounds.max,
                        BatchRadius = batch.batchRadius,
                        SpacingWithinBatch = batch.spacingWithinBatch,
                        GlobalSpacing = globalSpacing,
                        ItemsPerBatch = batch.itemsPerBatch,
                        ExistingPositions = allPositions,
                        OutputPositions = batchPositions,
                        RandomSeed = (uint)UnityEngine.Random.Range(1, int.MaxValue),
                        MaxAttempts = MAX_ATTEMPTS,
                        FloatingY = FLOATING_Y,
                        GridCounts = gridCounts,
                        GridWidth = gridWidth,
                        GridCellSize = GRID_CELL_SIZE
                    };

                    jobHandles.Add(batchJob.Schedule());
                    spawnData.Add((Vector3.zero, batch.prefabs[UnityEngine.Random.Range(0, batch.prefabs.Length)], batch.isPushable, spawnRotation));
                }
            }

            foreach (IndividualItemDefinition item in individualItems)
            {
                Quaternion spawnRotation = item.useCustomRotation
                    ? Quaternion.Euler(item.customRotation)
                    : plane.transform.rotation;

                for (int i = 0; i < item.numberToPlace; i++)
                {
                    NativeArray<Vector3> itemPosition = new NativeArray<Vector3>(1, Allocator.TempJob);
                    PlaceItemJob itemJob = new PlaceItemJob
                    {
                        BoundsMin = spawnAreaBounds.min,
                        BoundsMax = spawnAreaBounds.max,
                        GlobalSpacing = globalSpacing,
                        ExistingPositions = allPositions,
                        OutputPosition = itemPosition,
                        RandomSeed = (uint)UnityEngine.Random.Range(1, int.MaxValue),
                        MaxAttempts = MAX_ATTEMPTS,
                        FloatingY = FLOATING_Y,
                        GridCounts = gridCounts,
                        GridWidth = gridWidth,
                        GridCellSize = GRID_CELL_SIZE
                    };

                    jobHandles.Add(itemJob.Schedule());
                    spawnData.Add((Vector3.zero, item.prefab, false, spawnRotation));
                }
            }

            Profiler.BeginSample("CompleteJobs");
            JobHandle.CompleteAll(jobHandles.AsArray());
            Profiler.EndSample();

            int spawnDataIndex = 0;
            foreach (BatchDefinition batch in batches)
            {
                for (int i = 0; i < batch.numberOfBatches; i++)
                {
                    NativeArray<Vector3> batchPositions = new NativeArray<Vector3>(batch.itemsPerBatch, Allocator.TempJob);
                    bool validBatch = false;
                    for (int j = 0; j < batch.itemsPerBatch; j++)
                    {
                        if (allPositions.Length > spawnDataIndex && allPositions[spawnDataIndex] != Vector3.zero)
                        {
                            Vector3 pos = allPositions[spawnDataIndex];
                            spawnData[spawnDataIndex] = (pos, spawnData[spawnDataIndex].prefab, spawnData[spawnDataIndex].isPushable, spawnData[spawnDataIndex].rotation);
                            if (showDebugRays)
                                Debug.DrawRay(pos, Vector3.up * 5, Color.red, 5f);
                            validBatch = true;
                            spawnDataIndex++;
                        }
                    }
                    if (!validBatch)
                    {
                        ForcePlaceBatch(batch, spawnData, allPositions, spawnData[spawnDataIndex].rotation);
                    }
                    batchPositions.Dispose();
                }
            }

            foreach (IndividualItemDefinition item in individualItems)
            {
                for (int i = 0; i < item.numberToPlace; i++)
                {
                    if (allPositions.Length > spawnDataIndex && allPositions[spawnDataIndex] != Vector3.zero)
                    {
                        Vector3 pos = allPositions[spawnDataIndex];
                        spawnData[spawnDataIndex] = (pos, item.prefab, false, spawnData[spawnDataIndex].rotation);
                        if (showDebugRays)
                            Debug.DrawRay(pos, Vector3.up * 5, Color.blue, 5f);
                    }
                    else
                    {
                        ForcePlaceIndividualItem(item, spawnData, allPositions, spawnData[spawnDataIndex].rotation);
                    }
                    spawnDataIndex++;
                }
            }

            Profiler.BeginSample("ActivatePooledObjects");
            foreach (var (position, prefab, isPushable, rotation) in spawnData)
            {
                if (position == Vector3.zero) continue;
                if (prefab.GetComponent<Collider>() == null)
                    Debug.LogWarning($"Prefab {prefab.name} has no collider!");
                GameObject spawnedObject = GetPooledObject(prefab, position, rotation);
                SetupTrashObject(spawnedObject, isPushable);
            }
            Profiler.EndSample();

            allPositions.Dispose();
            jobHandles.Dispose();
            gridCounts.Dispose();
            Debug.Log($"Spawned {spawnData.Count} trash objects using Job System");
            Profiler.EndSample();
        }

        private void ForcePlaceBatch(BatchDefinition batch, List<(Vector3, GameObject, bool, Quaternion)> spawnData, NativeList<Vector3> allPositions, Quaternion spawnRotation)
        {
            Vector3 center = GenerateRandomPosition();
            List<Vector3> batchPositions = GenerateBatchPositions(center, batch);

            if (batchPositions == null)
            {
                Debug.LogWarning("Force placement failed: Unable to generate batch positions.");
                return;
            }

            foreach (Vector3 pos in batchPositions)
            {
                GameObject prefab = batch.prefabs[UnityEngine.Random.Range(0, batch.prefabs.Length)];
                allPositions.Add(pos);
                spawnData.Add((pos, prefab, batch.isPushable, spawnRotation));
                if (showDebugRays)
                    Debug.DrawRay(pos, Vector3.up * 5, Color.red, 5f);
            }
        }

        private void ForcePlaceIndividualItem(IndividualItemDefinition item, List<(Vector3, GameObject, bool, Quaternion)> spawnData, NativeList<Vector3> allPositions, Quaternion spawnRotation)
        {
            Vector3 position = GenerateRandomPosition();
            allPositions.Add(position);
            spawnData.Add((position, item.prefab, false, spawnRotation));
            if (showDebugRays)
                Debug.DrawRay(position, Vector3.up * 5, Color.blue, 5f);
            Debug.Log($"Force placed individual item at {position}");
        }

        private Vector3 GenerateRandomPosition()
        {
            Bounds bounds = spawnAreaBounds;
            for (int attempt = 0; attempt < MAX_ATTEMPTS; attempt++)
            {
                float x = UnityEngine.Random.Range(bounds.min.x, bounds.max.x);
                float z = UnityEngine.Random.Range(bounds.min.z, bounds.max.z);
                Vector3 candidate = new Vector3(x, FLOATING_Y, z);

                if (bounds.Contains(candidate))
                    return candidate;
            }

            Debug.LogWarning("Failed to find valid position within spawn area.");
            return new Vector3(planeCenter.x, FLOATING_Y, planeCenter.z);
        }

        private List<Vector3> GenerateBatchPositions(Vector3 center, BatchDefinition batch)
        {
            List<Vector3> positions = new List<Vector3>();

            for (int i = 0; i < batch.itemsPerBatch; i++)
            {
                for (int attempt = 0; attempt < MAX_ATTEMPTS; attempt++)
                {
                    Vector2 offset = UnityEngine.Random.insideUnitCircle * batch.batchRadius;
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

        private void SetupTrashObject(GameObject trash, bool isPushable)
        {
            int trashLayer = LayerMask.NameToLayer("Trash");
            if (trashLayer == -1)
            {
                Debug.LogError("Trash layer not found! Assigning Default layer.");
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

    [BurstCompile]
    public struct PlaceBatchJob : IJob
    {
        public Vector3 BoundsMin;
        public Vector3 BoundsMax;
        public float BatchRadius;
        public float SpacingWithinBatch;
        public float GlobalSpacing;
        public int ItemsPerBatch;
        public NativeList<Vector3> ExistingPositions;
        public NativeArray<Vector3> OutputPositions;
        public uint RandomSeed;
        public int MaxAttempts;
        public float FloatingY;
        public NativeArray<int> GridCounts;
        public int GridWidth;
        public float GridCellSize;

        public void Execute()
        {
            Unity.Mathematics.Random random = new Unity.Mathematics.Random(RandomSeed);
            Vector3 center = GenerateRandomPosition(random);
            bool centerValid = CheckCenterValid(center);

            if (!centerValid)
            {
                for (int i = 0; i < OutputPositions.Length; i++)
                    OutputPositions[i] = Vector3.zero;
                return;
            }

            NativeList<Vector3> batchPositions = new NativeList<Vector3>(Allocator.Temp);
            for (int i = 0; i < ItemsPerBatch; i++)
            {
                for (int attempt = 0; attempt < MaxAttempts; attempt++)
                {
                    Vector2 offset = RandomInsideUnitCircle(random) * BatchRadius;
                    Vector3 candidate = center + new Vector3(offset.x, 0, offset.y);
                    candidate.y = FloatingY;

                    if (!IsWithinBounds(candidate))
                        continue;

                    bool valid = true;
                    for (int j = 0; j < batchPositions.Length; j++)
                    {
                        if (Vector3.Distance(candidate, batchPositions[j]) < SpacingWithinBatch)
                        {
                            valid = false;
                            break;
                        }
                    }

                    if (valid && CheckCandidateValid(candidate))
                    {
                        batchPositions.Add(candidate);
                        UpdateGrid(candidate);
                        break;
                    }

                    if (attempt == MaxAttempts - 1)
                    {
                        batchPositions.Dispose();
                        for (int k = 0; k < OutputPositions.Length; k++)
                            OutputPositions[k] = Vector3.zero;
                        return;
                    }
                }
            }

            for (int i = 0; i < batchPositions.Length && i < OutputPositions.Length; i++)
            {
                OutputPositions[i] = batchPositions[i];
            }

            batchPositions.Dispose();
        }

        private bool IsWithinBounds(Vector3 pos)
        {
            return pos.x >= BoundsMin.x && pos.x <= BoundsMax.x &&
                   pos.z >= BoundsMin.z && pos.z <= BoundsMax.z;
        }

        private Vector3 GenerateRandomPosition(Unity.Mathematics.Random random)
        {
            for (int attempt = 0; attempt < MaxAttempts; attempt++)
            {
                float x = random.NextFloat(BoundsMin.x, BoundsMax.x);
                float z = random.NextFloat(BoundsMin.z, BoundsMax.z);
                Vector3 candidate = new Vector3(x, FloatingY, z);

                if (IsWithinBounds(candidate))
                    return candidate;
            }

            return new Vector3((BoundsMin.x + BoundsMax.x) / 2f, FloatingY, (BoundsMin.z + BoundsMax.z) / 2f);
        }

        private Vector2 RandomInsideUnitCircle(Unity.Mathematics.Random random)
        {
            float angle = random.NextFloat(0f, 2f * math.PI);
            float radius = random.NextFloat(0f, 1f);
            return new Vector2(math.cos(angle) * radius, math.sin(angle) * radius);
        }

        private bool CheckCenterValid(Vector3 center)
        {
            int gridX = Mathf.FloorToInt((center.x - BoundsMin.x) / GridCellSize);
            int gridZ = Mathf.FloorToInt((center.z - BoundsMin.z) / GridCellSize);
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dz = -1; dz <= 1; dz++)
                {
                    int x = gridX + dx;
                    int z = gridZ + dz;
                    if (x >= 0 && x < GridWidth && z >= 0 && z < GridCounts.Length / GridWidth)
                    {
                        int index = z * GridWidth + x;
                        if (GridCounts[index] > 0)
                            return false;
                    }
                }
            }
            return true;
        }

        private bool CheckCandidateValid(Vector3 candidate)
        {
            int gridX = Mathf.FloorToInt((candidate.x - BoundsMin.x) / GridCellSize);
            int gridZ = Mathf.FloorToInt((candidate.z - BoundsMin.z) / GridCellSize);
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dz = -1; dz <= 1; dz++)
                {
                    int x = gridX + dx;
                    int z = gridZ + dz;
                    if (x >= 0 && x < GridWidth && z >= 0 && z < GridCounts.Length / GridWidth)
                    {
                        int index = z * GridWidth + x;
                        if (GridCounts[index] > 0)
                            return false;
                    }
                }
            }
            return true;
        }

        private void UpdateGrid(Vector3 pos)
        {
            int gridX = Mathf.FloorToInt((pos.x - BoundsMin.x) / GridCellSize);
            int gridZ = Mathf.FloorToInt((pos.z - BoundsMin.z) / GridCellSize);
            if (gridX >= 0 && gridX < GridWidth && gridZ >= 0 && gridZ < GridCounts.Length / GridWidth)
            {
                int index = gridZ * GridWidth + gridX;
                GridCounts[index]++;
            }
        }
    }

    [BurstCompile]
    public struct PlaceItemJob : IJob
    {
        public Vector3 BoundsMin;
        public Vector3 BoundsMax;
        public float GlobalSpacing;
        public NativeList<Vector3> ExistingPositions;
        public NativeArray<Vector3> OutputPosition;
        public uint RandomSeed;
        public int MaxAttempts;
        public float FloatingY;
        public NativeArray<int> GridCounts;
        public int GridWidth;
        public float GridCellSize;

        public void Execute()
        {
            Unity.Mathematics.Random random = new Unity.Mathematics.Random(RandomSeed);
            Vector3 position = Vector3.zero;

            for (int attempt = 0; attempt < MaxAttempts; attempt++)
            {
                float x = random.NextFloat(BoundsMin.x, BoundsMax.x);
                float z = random.NextFloat(BoundsMin.z, BoundsMax.z);
                Vector3 candidate = new Vector3(x, FloatingY, z);

                if (candidate.x < BoundsMin.x || candidate.x > BoundsMax.x ||
                    candidate.z < BoundsMin.z || candidate.z > BoundsMax.z)
                    continue;

                if (CheckCandidateValid(candidate))
                {
                    position = candidate;
                    UpdateGrid(position);
                    break;
                }
            }

            OutputPosition[0] = position;
        }

        private bool CheckCandidateValid(Vector3 candidate)
        {
            int gridX = Mathf.FloorToInt((candidate.x - BoundsMin.x) / GridCellSize);
            int gridZ = Mathf.FloorToInt((candidate.z - BoundsMin.z) / GridCellSize);
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dz = -1; dz <= 1; dz++)
                {
                    int x = gridX + dx;
                    int z = gridZ + dz;
                    if (x >= 0 && x < GridWidth && z >= 0 && z < GridCounts.Length / GridWidth)
                    {
                        int index = z * GridWidth + x;
                        if (GridCounts[index] > 0)
                            return false;
                    }
                }
            }
            return true;
        }

        private void UpdateGrid(Vector3 pos)
        {
            int gridX = Mathf.FloorToInt((pos.x - BoundsMin.x) / GridCellSize);
            int gridZ = Mathf.FloorToInt((pos.z - BoundsMin.z) / GridCellSize);
            if (gridX >= 0 && gridX < GridWidth && gridZ >= 0 && gridZ < GridCounts.Length / GridWidth)
            {
                int index = gridZ * GridWidth + gridX;
                GridCounts[index]++;
            }
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
                rotationSpeed = UnityEngine.Random.Range(-10f, 10f),
                offset = UnityEngine.Random.Range(0f, 2f * Mathf.PI),
                isPushable = isPushable
            };
            floaters.Add(data);
        }

        void Update()
        {
            for (int i = 0; i < floaters.Count; i++)
            {
                var data = floaters[i];
                data.transform.Rotate(0, data.rotationSpeed * Time.deltaTime, 0);
            }
        }
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
                rotationSpeed = UnityEngine.Random.Range(-10f, 10f),
                offset = UnityEngine.Random.Range(0f, 2f * Mathf.PI),
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
