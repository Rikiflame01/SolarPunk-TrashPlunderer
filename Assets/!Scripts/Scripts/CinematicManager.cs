using UnityEngine;

public class CinematicManager : MonoBehaviour
{
    [Header("Camera Settings")]
    [SerializeField] private Camera cinematicCamera;
    [SerializeField] private Transform lookAtPoint;
    [SerializeField] private float rotationSpeed = 30f;
    [SerializeField] private float rotationDistance = 5f;
    [SerializeField] private float rotationHeight = 2f;
    [SerializeField] private float totalRotationDegrees = 360f;

    [Header("Movement Settings")]
    [SerializeField] private Transform targetPosition;
    [SerializeField] private float moveSpeed = 5f;

    [Header("Game Objects to Activate")]
    [SerializeField] private GameObject[] objectsToEnable;

    private float currentRotation = 0f;
    private bool isRotating = true;
    private bool isMoving = false;

    void Start()
    {
        if (cinematicCamera == null || lookAtPoint == null || targetPosition == null)
        {
            Debug.LogError("Cinematic Manager: Required references are missing!");
            enabled = false;
            return;
        }

        // Ensure camera is enabled at start
        cinematicCamera.enabled = true;
        PositionCamera();
    }

    void Update()
    {
        if (isRotating)
        {
            RotateCamera();
        }
        else if (isMoving)
        {
            MoveCameraToTarget();
        }
    }

    void PositionCamera()
    {
        // Set initial camera position based on lookAtPoint
        Vector3 offset = new Vector3(0f, rotationHeight, -rotationDistance);
        cinematicCamera.transform.position = lookAtPoint.position + offset;
        cinematicCamera.transform.LookAt(lookAtPoint);
    }

    void RotateCamera()
    {
        // Rotate camera around lookAtPoint
        currentRotation += rotationSpeed * Time.deltaTime;
        float angle = currentRotation % 360f;
        Vector3 offset = new Vector3(
            Mathf.Sin(angle * Mathf.Deg2Rad) * rotationDistance,
            rotationHeight,
            Mathf.Cos(angle * Mathf.Deg2Rad) * rotationDistance
        );

        cinematicCamera.transform.position = lookAtPoint.position + offset;
        cinematicCamera.transform.LookAt(lookAtPoint);

        // Check if rotation is complete
        if (currentRotation >= totalRotationDegrees)
        {
            isRotating = false;
            isMoving = true;
        }
    }

    void MoveCameraToTarget()
    {
        // Move camera towards target position
        Vector3 direction = (targetPosition.position - cinematicCamera.transform.position).normalized;
        cinematicCamera.transform.position += direction * moveSpeed * Time.deltaTime;
        cinematicCamera.transform.LookAt(lookAtPoint);

        // Check if camera is close enough to target
        if (Vector3.Distance(cinematicCamera.transform.position, targetPosition.position) < 0.1f)
        {
            // Snap to final position
            cinematicCamera.transform.position = targetPosition.position;
            CompleteCinematic();
        }
    }

    void CompleteCinematic()
    {
        // Disable camera and enable specified game objects
        cinematicCamera.enabled = false;

        foreach (GameObject obj in objectsToEnable)
        {
            if (obj != null)
            {
                obj.SetActive(true);
            }
        }

        // Disable this script to prevent further updates
        enabled = false;
    }
}