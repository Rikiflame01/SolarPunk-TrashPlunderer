using UnityEngine;

// this was to test the spawning of objects on a plane, before using more complex methods 
// like perlin noise or other procedural generation methods.
public class SimplePlaneSpawner : MonoBehaviour
{
    [SerializeField, Tooltip("The plane GameObject representing the ocean floor")]
    private GameObject plane;

    [SerializeField, Tooltip("The prefab to spawn (e.g., starfish or coral)")]
    private GameObject prefabToSpawn;

    [SerializeField, Tooltip("Number of objects to spawn")]
    private int numberToSpawn = 20;

    [SerializeField, Tooltip("Use raycasting to place objects on the plane's surface")]
    private bool useRaycast = true;

    private Vector3 planeCenter;
    private Vector3 planeExtents;
    private float planeY;

    void Start()
    {
        CalculatePlaneBounds();
        SpawnObjects();
    }

    // Calculate the world bounds of the plane
    private void CalculatePlaneBounds()
    {
        if (plane == null)
        {
            Debug.LogError("Plane reference is not set in the Inspector!");
            return;
        }

        if (prefabToSpawn == null)
        {
            Debug.LogError("Prefab to spawn is not set in the Inspector!");
            return;
        }

        Mesh mesh = plane.GetComponent<MeshFilter>().sharedMesh;
        Bounds localBounds = mesh.bounds;
        planeCenter = plane.transform.TransformPoint(localBounds.center);
        planeExtents = (plane.transform.TransformPoint(localBounds.max) - plane.transform.TransformPoint(localBounds.min)) * 0.5f;
        planeY = planeCenter.y;

        Debug.Log($"Plane bounds: Center={planeCenter}, Extents={planeExtents}, Y={planeY}");
        Debug.Log($"Prefab scale: {prefabToSpawn.transform.localScale}");
    }

    // Spawn the specified number of objects
    private void SpawnObjects()
    {
        // Get the plane's rotation and prefab's scale
        Quaternion spawnRotation = plane.transform.rotation * Quaternion.Euler(-90, 0, 0);
        Vector3 prefabScale = prefabToSpawn.transform.localScale;

        for (int i = 0; i < numberToSpawn; i++)
        {
            Vector3 position = GenerateRandomPosition();
            GameObject spawnedObject = Instantiate(prefabToSpawn, position, spawnRotation); // No parent
            spawnedObject.transform.localScale = prefabScale; // Explicitly set prefab's scale
            Debug.Log($"Spawned object {i + 1} at {position} with rotation {spawnRotation.eulerAngles}, localScale {spawnedObject.transform.localScale}, world scale {spawnedObject.transform.lossyScale}");
        }
    }

    // Generate a random position within the plane's bounds
    private Vector3 GenerateRandomPosition()
    {
        float x = Random.Range(planeCenter.x - planeExtents.x, planeCenter.x + planeExtents.x);
        float z = Random.Range(planeCenter.z - planeExtents.z, planeCenter.z + planeExtents.z);

        if (useRaycast)
        {
            // Raycast from above the plane to find the surface
            Vector3 candidate = new Vector3(x, planeCenter.y + 10f, z);
            Ray ray = new Ray(candidate, plane.transform.rotation * Vector3.down);
            if (Physics.Raycast(ray, out RaycastHit hit, 20f))
            {
                if (hit.collider.gameObject == plane)
                    return hit.point;
            }
            Debug.LogWarning($"Raycast missed plane at X={x}, Z={z}, using fallback position.");
        }

        // Fallback to plane's Y position if raycast is disabled or fails
        return new Vector3(x, planeY, z);
    }
}