using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TrashInteractionSystem : MonoBehaviour
{
    [SerializeField, Tooltip("Player data ScriptableObject")]
    private PlayerData playerData;

    [SerializeField, Tooltip("Main camera for canvas orientation")]
    private Camera mainCamera;

    [SerializeField, Tooltip("Interaction canvas prefab")]
    private InteractionCanvas canvasPrefab;

    [SerializeField, Tooltip("Detection radius for trash objects")]
    private float detectionRadius = 5f;

    [SerializeField, Tooltip("Layer mask for Trash")]
    private LayerMask trashLayer;

    [SerializeField, Tooltip("Layer mask for UnderWaterTrash")]
    private LayerMask underwaterTrashLayer;

    [SerializeField, Tooltip("Update interval for detection (seconds)")]
    private float detectionInterval = 0.2f;

    [SerializeField, Tooltip("Update interval for closest trash check (seconds)")]
    private float closestTrashUpdateInterval = 0.1f;

    private List<InteractableTrash> nearbyTrash = new List<InteractableTrash>(32);
    private InteractableTrash closestTrash;
    private InteractionCanvas canvasInstance;
    private Transform cameraTransform;
    private float detectionTimer;
    private Collider[] hitBuffer = new Collider[32];
    private bool isCanvasSetup;
    private Coroutine closestTrashCoroutine;

    public InteractableTrash ClosestTrash => closestTrash;
    public InteractionCanvas CanvasInstance => canvasInstance;

    void Start()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError("No main camera found! Assign a camera in the Inspector.");
                enabled = false;
                return;
            }
        }
        cameraTransform = mainCamera.transform;

        if (canvasPrefab == null)
        {
            Debug.LogError("InteractionCanvas prefab not assigned!");
            enabled = false;
            return;
        }

        // Start batched trash initialization
        StartCoroutine(BatchInitializeTrash());
        // Start detection coroutine
        StartCoroutine(DetectNearbyTrashCoroutine());
    }

    private IEnumerator BatchInitializeTrash()
    {
        InteractableTrash[] allTrash = FindObjectsByType<InteractableTrash>(FindObjectsSortMode.None);
        int batchSize = 10; // Initialize 10 trash objects per frame
        for (int i = 0; i < allTrash.Length; i += batchSize)
        {
            for (int j = i; j < Mathf.Min(i + batchSize, allTrash.Length); j++)
            {
                if (allTrash[j] != null && !allTrash[j].IsActive)
                    allTrash[j].Initialize();
            }
            yield return null; // Spread over frames
        }
        Debug.Log($"Initialized {allTrash.Length} trash objects");
    }

    private IEnumerator DetectNearbyTrashCoroutine()
    {
        while (true)
        {
            DetectNearbyTrash();
            if (nearbyTrash.Count > 0 && !isCanvasSetup)
            {
                SetupCanvas();
            }
            yield return new WaitForSeconds(detectionInterval);
        }
    }

    private void DetectNearbyTrash()
    {
        int hitCount = Physics.OverlapSphereNonAlloc(transform.position, detectionRadius, hitBuffer, trashLayer | underwaterTrashLayer);

        nearbyTrash.Clear();
        for (int i = 0; i < hitCount; i++)
        {
            InteractableTrash trash = hitBuffer[i].GetComponent<InteractableTrash>();
            if (trash != null && trash.IsActive && !nearbyTrash.Contains(trash))
            {
                nearbyTrash.Add(trash);
            }
        }
        Debug.Log($"Detected {nearbyTrash.Count} trash objects within {detectionRadius}m");
    }

    private void SetupCanvas()
    {
        if (isCanvasSetup)
            return;

        canvasInstance = Instantiate(canvasPrefab, Vector3.zero, Quaternion.identity);
        canvasInstance.Initialize(cameraTransform);
        isCanvasSetup = true;
        // Start closest trash update coroutine
        closestTrashCoroutine = StartCoroutine(UpdateClosestTrashCoroutine());
        Debug.Log("InteractionCanvas setup complete");
    }

    private IEnumerator UpdateClosestTrashCoroutine()
    {
        while (true)
        {
            UpdateClosestTrash();
            yield return new WaitForSeconds(closestTrashUpdateInterval);
        }
    }

    private void UpdateClosestTrash()
    {
        InteractableTrash newClosestTrash = null;
        Vector3 canvasPosition = Vector3.zero;
        string prompt = "";

        float minDistance = float.MaxValue;
        Vector3 playerPos = transform.position;
        foreach (var trash in nearbyTrash)
        {
            if (trash == null || !trash.IsActive)
                continue;

            if (trash.gameObject.layer == LayerMask.NameToLayer("Trash"))
            {
                float distance = Vector3.Distance(playerPos, trash.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    newClosestTrash = trash;
                    prompt = "Press E";
                }
            }
        }

        if (newClosestTrash == null)
        {
            foreach (var trash in nearbyTrash)
            {
                if (trash == null || !trash.IsActive)
                    continue;

                if (trash.gameObject.layer == LayerMask.NameToLayer("UnderWaterTrash"))
                {
                    float distance = Vector3.Distance(playerPos, trash.transform.position);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        newClosestTrash = trash;
                        prompt = "Hold E";
                    }
                }
            }
        }

        if (newClosestTrash != closestTrash || !string.IsNullOrEmpty(prompt))
        {
            if (closestTrash != null && (newClosestTrash != closestTrash || string.IsNullOrEmpty(prompt)))
            {
                if (canvasInstance != null && !canvasInstance.IsShowingTempMessage)
                    canvasInstance.HidePrompt();
            }

            closestTrash = newClosestTrash;

            if (closestTrash != null && !string.IsNullOrEmpty(prompt) && canvasInstance != null && !canvasInstance.IsShowingTempMessage)
            {
                canvasPosition = closestTrash.GetCanvasPosition();
                canvasInstance.ShowPrompt(prompt, canvasPosition);
                Debug.Log($"Showing canvas for {closestTrash.gameObject.name} with prompt: {prompt} at {canvasPosition}");
            }
        }
        else if (closestTrash != null && string.IsNullOrEmpty(prompt) && canvasInstance != null && !canvasInstance.IsShowingTempMessage)
        {
            canvasInstance.HidePrompt();
        }

        // Disable canvas if no trash is nearby
        if (nearbyTrash.Count == 0 && canvasInstance != null)
            canvasInstance.HidePrompt();
    }

    public void OnTrashCollected(InteractableTrash collectedTrash)
    {
        if (collectedTrash != null)
        {
            nearbyTrash.Remove(collectedTrash);
            if (closestTrash == collectedTrash)
            {
                closestTrash = null;
                if (canvasInstance != null && !canvasInstance.IsShowingTempMessage)
                    canvasInstance.HidePrompt();
            }
            DetectNearbyTrash(); // Immediate refresh
        }
    }

    void OnDestroy()
    {
        if (canvasInstance != null)
            Destroy(canvasInstance.gameObject);
        if (closestTrashCoroutine != null)
            StopCoroutine(closestTrashCoroutine);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}