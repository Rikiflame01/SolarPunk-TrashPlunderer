using UnityEngine;

public class InteractableTrash : MonoBehaviour
{
    [SerializeField] 
    private Collider col;

    [SerializeField]
    private TrashProperties trashProperties;

    private bool isInitialized;

    void Awake()
    {
        if (!isInitialized)
            Initialize();
    }

    public void Initialize()
    {
        if (col == null)
        {
            col = GetComponent<Collider>();
            if (col == null)
                Debug.LogError($"No collider found on {gameObject.name}!");
        }

        if (trashProperties == null)
        {
            trashProperties = GetComponent<TrashProperties>();
            if (trashProperties == null)
                Debug.LogError($"No TrashProperties found on {gameObject.name}! Please add one.");
        }

        isInitialized = true;
    }

    public Vector3 GetCanvasPosition()
    {
        if (col is SphereCollider sphere)
            return transform.position + Vector3.up * (sphere.radius + 0.5f);
        if (col is BoxCollider box)
            return transform.position + Vector3.up * (box.size.y * 0.5f + 0.5f);
        return transform.position + Vector3.up * 1f;
    }

    public TrashProperties TrashProperties => trashProperties;

    // Optional: Allow external systems to check if this trash is active
    public bool IsActive => gameObject.activeInHierarchy && isInitialized;
}