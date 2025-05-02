using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [SerializeField, Tooltip("Player data ScriptableObject")]
    private PlayerData playerData;

    [SerializeField, Tooltip("Rotate boat to face movement direction (yaw)")]
    private bool rotateToMovement = true;

    [SerializeField, Tooltip("Rotation speed in degrees per second for yaw")]
    private float rotationSpeed = 720f;

    [SerializeField, Tooltip("Main camera for camera-relative movement")]
    private Camera mainCamera;

    [SerializeField, Tooltip("Acceleration for easing in/out (meters per second squared)")]
    private float acceleration = 10f;

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

    [SerializeField, Tooltip("Trash interaction system")]
    private TrashInteractionSystem trashInteractionSystem;

    [SerializeField, Tooltip("Enable debug logs")]
    private bool debug = false;

    private Rigidbody rb;
    private Vector3 moveInput;
    private Vector3 currentVelocity;
    private float time;
    private float holdTimer;
    private bool isHoldingE;
    private float storageFullCooldown; // Cooldown for "Storage Full" message

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.constraints = RigidbodyConstraints.FreezeRotationY;

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
                Debug.LogError("No main camera found! Assign a camera in the Inspector.");
        }

        if (playerData == null)
            Debug.LogError("PlayerData ScriptableObject not assigned!");

        if (trashInteractionSystem == null)
            Debug.LogError("TrashInteractionSystem not assigned!");

        time = Random.Range(0f, 10f);

        ActionManager.OnTrashCollected += AddTrashPoints;
    }

    void OnDestroy()
    {
        ActionManager.OnTrashCollected -= AddTrashPoints;
    }

    void Update()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        moveInput = new Vector3(horizontal, 0f, vertical).normalized;

        if (debug && moveInput.magnitude > 0.1f)
            Debug.Log($"Input: {moveInput}, Magnitude: {moveInput.magnitude}");

        if (storageFullCooldown > 0f)
            storageFullCooldown -= Time.deltaTime;

        HandleTrashInteraction();
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
        Vector3 moveDirection = CameraRelativeDirection(moveInput);
        float speed = playerData != null ? playerData.PlayerSpeed : 5f;
        Vector3 targetVelocity = moveDirection * speed;

        currentVelocity = Vector3.MoveTowards(currentVelocity, targetVelocity, acceleration * Time.fixedDeltaTime);
        rb.linearVelocity = new Vector3(currentVelocity.x, rb.linearVelocity.y, currentVelocity.z);

        if (debug)
            Debug.Log($"Velocity: {rb.linearVelocity}, Target Velocity: {targetVelocity}");
    }

    private void ApplyBuoyancyAndWaterMotion()
    {
        time += Time.fixedDeltaTime;

        float yError = targetY - transform.position.y;
        float buoyancy = yError * buoyancyForce - rb.linearVelocity.y * 2f;
        rb.AddForce(Vector3.up * buoyancy, ForceMode.Acceleration);

        if (bobAmplitude > 0)
        {
            float bobOffset = Mathf.Sin(time * bobFrequency * 2f * Mathf.PI) * bobAmplitude;
            rb.AddForce(Vector3.up * bobOffset * buoyancyForce, ForceMode.Acceleration);
        }

        if (rollAmplitude > 0 || pitchAmplitude > 0)
        {
            float roll = Mathf.Sin(time * rollFrequency * 2f * Mathf.PI) * rollAmplitude;
            float pitch = Mathf.Cos(time * pitchFrequency * 2f * Mathf.PI) * pitchAmplitude;
            Quaternion targetRotation = Quaternion.Euler(pitch, rb.rotation.eulerAngles.y, roll);
            rb.rotation = Quaternion.Slerp(rb.rotation, targetRotation, Time.fixedDeltaTime * 2f);
        }
    }

    private void RotatePlayer()
    {
        Vector3 moveDirection = CameraRelativeDirection(moveInput);
        Quaternion targetYaw = Quaternion.LookRotation(moveDirection, Vector3.up);
        Vector3 euler = targetYaw.eulerAngles;
        euler.x = rb.rotation.eulerAngles.x;
        euler.z = rb.rotation.eulerAngles.z;
        targetYaw = Quaternion.Euler(euler);
        rb.rotation = Quaternion.RotateTowards(rb.rotation, targetYaw, rotationSpeed * Time.fixedDeltaTime);
    }

    private Vector3 CameraRelativeDirection(Vector3 input)
    {
        if (mainCamera == null)
            return input;

        Vector3 camForward = mainCamera.transform.forward;
        camForward.y = 0f;
        camForward.Normalize();

        Vector3 camRight = mainCamera.transform.right;
        camRight.y = 0f;
        camRight.Normalize();

        return (camForward * input.z + camRight * input.x).normalized;
    }

    private void HandleTrashInteraction()
    {
        if (trashInteractionSystem == null || trashInteractionSystem.ClosestTrash == null)
            return;

        InteractableTrash closest = trashInteractionSystem.ClosestTrash;
        TrashProperties properties = closest.TrashProperties;

        if (properties == null)
            return;

        int points = properties.GetPoints();
        bool canCollect = playerData != null && playerData.CurrentTrash + points <= playerData.PlayerStorage;

        // Surface Trash: Tap E to collect
        if (closest.gameObject.layer == LayerMask.NameToLayer("Trash") && Input.GetKeyDown(KeyCode.E))
        {
            if (canCollect)
            {
                ActionManager.InvokeTrashCollected(points);
                trashInteractionSystem.OnTrashCollected(closest);
                Destroy(closest.gameObject);
                if (debug)
                    Debug.Log($"Collected surface trash {closest.gameObject.name} for {points} points");
            }
            else if (storageFullCooldown <= 0f)
            {
                trashInteractionSystem.CanvasInstance.ShowTempMessage("Storage Full", 1f);
                storageFullCooldown = 1f; // Prevent spamming message
                if (debug)
                    Debug.Log("Cannot collect surface trash: Storage full");
            }
        }
        // UnderWaterTrash: Hold E for specific duration
        else if (closest.gameObject.layer == LayerMask.NameToLayer("UnderWaterTrash"))
        {
            if (Input.GetKey(KeyCode.E))
            {
                isHoldingE = true;
                holdTimer += Time.deltaTime;
                float requiredHoldTime = properties.GetHoldTime();
                if (holdTimer >= requiredHoldTime)
                {
                    if (canCollect)
                    {
                        ActionManager.InvokeTrashCollected(points);
                        trashInteractionSystem.OnTrashCollected(closest);
                        Destroy(closest.gameObject);
                        if (debug)
                            Debug.Log($"Collected underwater trash {closest.gameObject.name} for {points} points after holding E for {holdTimer}s");
                        holdTimer = 0f;
                        isHoldingE = false;
                    }
                    else if (storageFullCooldown <= 0f)
                    {
                        trashInteractionSystem.CanvasInstance.ShowTempMessage("Storage Full", 1f);
                        storageFullCooldown = 1f; // Prevent spamming message
                        holdTimer = 0f;
                        isHoldingE = false;
                        if (debug)
                            Debug.Log("Cannot collect underwater trash: Storage full");
                    }
                }
            }
            else
            {
                if (isHoldingE)
                {
                    holdTimer = 0f;
                    isHoldingE = false;
                    if (debug)
                        Debug.Log("Released E before collecting underwater trash");
                }
            }
        }
    }

    private void AddTrashPoints(int points)
    {
        if (playerData != null)
        {
            playerData.CurrentTrash += points;
            if (debug)
                Debug.Log($"Added {points} to CurrentTrash. Total: {playerData.CurrentTrash}");
        }
    }
}