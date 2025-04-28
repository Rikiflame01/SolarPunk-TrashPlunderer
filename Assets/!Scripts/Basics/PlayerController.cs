using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [SerializeField, Tooltip("Movement speed in units per second")]
    private float moveSpeed = 5f;

    [SerializeField, Tooltip("Rotate boat to face movement direction (yaw)")]
    private bool rotateToMovement = true;

    [SerializeField, Tooltip("Rotation speed in degrees per second for yaw")]
    private float rotationSpeed = 720f;

    [SerializeField, Tooltip("Main camera for camera-relative movement")]
    private Camera mainCamera;

    [SerializeField, Tooltip("Smoothing factor for velocity changes (0 = instant, 1 = very smooth)")]
    private float velocitySmoothing = 0.1f;

    [SerializeField, Tooltip("Target Y position for the boat (water surface)")]
    private float targetY = 2f;

    [SerializeField, Tooltip("Buoyancy strength to keep boat at target Y")]
    private float buoyancyForce = 20f;

    [SerializeField, Tooltip("Amplitude of vertical bobbing (meters)")]
    private float bobAmplitude = 0.2f;

    [SerializeField, Tooltip("Frequency of vertical bobbing (cycles per second)")]
    private float bobFrequency = 1f;

    [SerializeField, Tooltip("Amplitude of roll (degrees)")]
    private float rollAmplitude = 5f;

    [SerializeField, Tooltip("Frequency of roll (cycles per second)")]
    private float rollFrequency = 0.5f;

    [SerializeField, Tooltip("Amplitude of pitch (degrees)")]
    private float pitchAmplitude = 3f;

    [SerializeField, Tooltip("Frequency of pitch (cycles per second)")]
    private float pitchFrequency = 0.7f;

    private Rigidbody rb;
    private Vector3 moveInput;
    private Vector3 targetVelocity;
    private float time;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false; // Disable gravity for buoyancy control
        rb.interpolation = RigidbodyInterpolation.Interpolate; // Smooth movement
        rb.constraints = RigidbodyConstraints.FreezeRotationY; // Allow roll/pitch, control yaw

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
                Debug.LogError("No main camera found! Assign a camera in the Inspector.");
        }

        // Initialize time for water motion
        time = Random.Range(0f, 10f); // Random start for varied motion
    }

    void Update()
    {
        // Get WASD/Arrow key input (smooth transitions)
        float horizontal = Input.GetAxis("Horizontal"); // A/D or Left/Right
        float vertical = Input.GetAxis("Vertical"); // W/S or Up/Down
        moveInput = new Vector3(horizontal, 0f, vertical).normalized;

        // Log input for debugging
        if (moveInput.magnitude > 0.1f)
            Debug.Log($"Input: {moveInput}, Magnitude: {moveInput.magnitude}");
    }

    void FixedUpdate()
    {
        MovePlayer();
        ApplyBuoyancyAndWaterMotion();
        if (rotateToMovement && moveInput.magnitude > 0.1f)
            RotatePlayer();
    }

    private void MovePlayer()
    {
        // Transform input to camera-relative direction
        Vector3 moveDirection = CameraRelativeDirection(moveInput);

        // Calculate target velocity (XZ plane)
        Vector3 desiredVelocity = moveDirection * moveSpeed;

        // Smoothly interpolate to target velocity
        targetVelocity = Vector3.Lerp(targetVelocity, desiredVelocity, 1f - Mathf.Pow(velocitySmoothing, Time.fixedDeltaTime * 60f));
        targetVelocity.y = rb.linearVelocity.y; // Preserve vertical velocity
        rb.linearVelocity = new Vector3(targetVelocity.x, rb.linearVelocity.y, targetVelocity.z);

        // Log velocity for debugging
        Debug.Log($"Velocity: {rb.linearVelocity}, Target Velocity: {targetVelocity}");
    }

    private void ApplyBuoyancyAndWaterMotion()
    {
        time += Time.fixedDeltaTime;

        // Buoyancy: Apply force to keep boat near targetY
        float yError = targetY - transform.position.y;
        float buoyancy = yError * buoyancyForce - rb.linearVelocity.y * 2f; // Damping
        rb.AddForce(Vector3.up * buoyancy, ForceMode.Acceleration);

        // Vertical bobbing (sinking/rising)
        float bobOffset = Mathf.Sin(time * bobFrequency * 2f * Mathf.PI) * bobAmplitude;
        rb.AddForce(Vector3.up * bobOffset * buoyancyForce, ForceMode.Acceleration);

        // Calculate roll and pitch rotations
        float roll = Mathf.Sin(time * rollFrequency * 2f * Mathf.PI) * rollAmplitude;
        float pitch = Mathf.Cos(time * pitchFrequency * 2f * Mathf.PI) * pitchAmplitude;

        // Apply roll and pitch (local X and Z rotations)
        Quaternion targetRotation = Quaternion.Euler(pitch, rb.rotation.eulerAngles.y, roll);
        rb.rotation = Quaternion.Slerp(rb.rotation, targetRotation, Time.fixedDeltaTime * 2f);
    }

    private void RotatePlayer()
    {
        // Get camera-relative movement direction
        Vector3 moveDirection = CameraRelativeDirection(moveInput);

        // Calculate target yaw rotation
        Quaternion targetYaw = Quaternion.LookRotation(moveDirection, Vector3.up);

        // Preserve current roll and pitch, only update yaw
        Vector3 euler = targetYaw.eulerAngles;
        euler.x = rb.rotation.eulerAngles.x;
        euler.z = rb.rotation.eulerAngles.z;
        targetYaw = Quaternion.Euler(euler);

        // Smoothly rotate to face movement direction
        rb.rotation = Quaternion.RotateTowards(
            rb.rotation,
            targetYaw,
            rotationSpeed * Time.fixedDeltaTime
        );
    }

    private Vector3 CameraRelativeDirection(Vector3 input)
    {
        if (mainCamera == null)
            return input;

        // Get camera's forward and right vectors, projected onto XZ plane
        Vector3 camForward = mainCamera.transform.forward;
        camForward.y = 0f;
        camForward.Normalize();

        Vector3 camRight = mainCamera.transform.right;
        camRight.y = 0f;
        camRight.Normalize();

        // Transform input to camera-relative direction
        return (camForward * input.z + camRight * input.x).normalized;
    }
}