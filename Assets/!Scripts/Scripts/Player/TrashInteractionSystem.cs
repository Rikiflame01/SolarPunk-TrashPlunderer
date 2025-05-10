using UnityEngine;
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

    private List<InteractableTrash> nearbyTrash = new List<InteractableTrash>(32);
    private InteractableTrash closestTrash;
    private InteractionCanvas canvasInstance;
    private Transform cameraTransform;
    private float detectionTimer;
    private Collider[] hitBuffer = new Collider[32];

    public InteractableTrash ClosestTrash => closestTrash;
    public InteractionCanvas CanvasInstance => canvasInstance; // Expose for PlayerController

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

        canvasInstance = Instantiate(canvasPrefab, Vector3.zero, Quaternion.identity);
        canvasInstance.Initialize(cameraTransform);

        DetectNearbyTrash();
    }

    void Update()
    {
        detectionTimer += Time.deltaTime;
        if (detectionTimer >= detectionInterval)
        {
            DetectNearbyTrash();
            detectionTimer = 0f;
        }

        UpdateClosestTrash();
    }

    private void DetectNearbyTrash()
    {
        int hitCount = Physics.OverlapSphereNonAlloc(transform.position, detectionRadius, hitBuffer, trashLayer | underwaterTrashLayer);

        nearbyTrash.Clear();
        for (int i = 0; i < hitCount; i++)
        {
            InteractableTrash trash = hitBuffer[i].GetComponent<InteractableTrash>();
            if (trash != null && !nearbyTrash.Contains(trash))
            {
                nearbyTrash.Add(trash);
            }
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
            if (trash == null || trash.gameObject == null)
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
                if (trash == null || trash.gameObject == null)
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
                canvasInstance.HidePrompt();
            }

            closestTrash = newClosestTrash;

            if (closestTrash != null && !string.IsNullOrEmpty(prompt))
            {
                canvasPosition = closestTrash.GetCanvasPosition();
                canvasInstance.ShowPrompt(prompt, canvasPosition);
            }
        }
        else if (closestTrash != null && string.IsNullOrEmpty(prompt))
        {
            canvasInstance.HidePrompt();
        }
    }

    public void OnTrashCollected(InteractableTrash collectedTrash)
    {
        if (collectedTrash != null)
        {
            nearbyTrash.Remove(collectedTrash);
            if (closestTrash == collectedTrash)
            {
                closestTrash = null;
                canvasInstance.HidePrompt();
            }
            DetectNearbyTrash();
        }
    }

    void OnDestroy()
    {
        if (canvasInstance != null)
            Destroy(canvasInstance.gameObject);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}