using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField, Tooltip("The player or target to follow")]
    private Transform target;

    [SerializeField, Tooltip("Initial offset from the target (local space)")]
    private Vector3 offset = new Vector3(0, 5, -7);

    [SerializeField, Tooltip("Speed for smoothing camera movement")]
    private float smoothSpeed = 0.125f;

    [SerializeField, Tooltip("Mouse sensitivity for yaw (horizontal) rotation")]
    private float yawSensitivity = 200f;

    [SerializeField, Tooltip("Mouse sensitivity for pitch (vertical) rotation")]
    private float pitchSensitivity = 200f;

    [SerializeField, Tooltip("Minimum pitch angle (degrees, negative for down)")]
    private float minPitch = -30f;

    [SerializeField, Tooltip("Maximum pitch angle (degrees, positive for up)")]
    private float maxPitch = 60f;

    [SerializeField, Tooltip("Layer mask for ground collision detection")]
    private LayerMask groundLayer;

    private float currentYaw;
    private float currentPitch;

    void Start()
    {
        // Initialize yaw and pitch based on initial offset
        Vector3 offsetDir = offset.normalized;
        currentPitch = Mathf.Asin(offsetDir.y) * Mathf.Rad2Deg;
        currentYaw = Mathf.Atan2(offsetDir.x, offsetDir.z) * Mathf.Rad2Deg;

        // Ensure ground layer is set
        if (groundLayer.value == 0)
        {
            int groundLayerIndex = LayerMask.NameToLayer("Ground");
            if (groundLayerIndex >= 0)
                groundLayer = 1 << groundLayerIndex;
            else
                Debug.LogWarning("Ground layer not found! Please assign the Ground layer in the Inspector.");
        }
    }

    void LateUpdate()
    {
        if (target == null)
            return;

        // Get mouse input for rotation
        currentYaw += Input.GetAxis("Mouse X") * yawSensitivity * Time.deltaTime;
        currentPitch -= Input.GetAxis("Mouse Y") * pitchSensitivity * Time.deltaTime;
        currentPitch = Mathf.Clamp(currentPitch, minPitch, maxPitch);

        // Calculate desired camera position
        Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0);
        Vector3 desiredOffset = rotation * (offset.magnitude * Vector3.back);
        Vector3 desiredPosition = target.position + desiredOffset;

        // Adjust position to avoid ground collision
        Vector3 adjustedPosition = AdjustForGroundCollision(target.position, desiredPosition);

        // Smoothly move to adjusted position
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, adjustedPosition, smoothSpeed);
        transform.position = smoothedPosition;

        // Look at the target
        transform.LookAt(target);
    }

    private Vector3 AdjustForGroundCollision(Vector3 targetPos, Vector3 desiredPos)
    {
        // Raycast from target to desired camera position
        Vector3 direction = (desiredPos - targetPos).normalized;
        float distance = Vector3.Distance(targetPos, desiredPos);
        Ray ray = new Ray(targetPos, direction);

        if (Physics.Raycast(ray, out RaycastHit hit, distance, groundLayer))
        {
            // Move camera just before the hit point to avoid clipping
            return hit.point - direction * 0.1f; // Small offset to prevent clipping
        }

        // No collision, use desired position
        return desiredPos;
    }
}