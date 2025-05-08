using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [SerializeField, Tooltip("Player data ScriptableObject")]
    private PlayerData playerData;

    [SerializeField, Tooltip("Light manager for time of day")]
    private LightManager lightManager;

    private float hpRegenAccumulator = 0f;
    private float energyRegenAccumulator = 0f;

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

    [SerializeField, Tooltip("Speed of rotation correction after collision (higher is faster)")]
    private float collisionCorrectionSpeed = 5f;

    [SerializeField, Tooltip("Time interval for energy drain while moving (seconds per 1 energy point)")]
    private float energyDrainInterval = 3f;

    public bool IsMovementEnabled { get; set; } = true;

    private Rigidbody rb;
    private Vector3 moveInput;
    private Vector3 currentVelocity;
    private float time;
    private float holdTimer;
    private bool isHoldingE;
    private float storageFullCooldown;
    private bool isCorrectingToNaturalAngle;
    private Quaternion naturalRotation;
    private float energyDrainTimer;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.constraints = RigidbodyConstraints.FreezeRotationY;
        rb.linearDamping = 0.5f;
        rb.angularDamping = 2f;

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
                Debug.LogError("No main camera found! Assign a camera in the Inspector.");
        }

        if (playerData == null)
            Debug.LogError("PlayerData ScriptableObject not assigned!");
        else if (debug)
            Debug.Log($"PlayerData assigned: HP={playerData.PlayerHP}, Energy={playerData.PlayerEnergy}");

        if (trashInteractionSystem == null)
            Debug.LogError("TrashInteractionSystem not assigned!");

        if (lightManager == null)
            Debug.LogError("LightManager not assigned! Energy regeneration will not be time-restricted.");

        time = Random.Range(0f, 10f);
        isCorrectingToNaturalAngle = false;
        naturalRotation = Quaternion.Euler(-5f, 0f, 0f);
        energyDrainTimer = 0f;

        ActionManager.OnTrashCollected += AddTrashPoints;
        GameManager.OnGameStateChanged += HandleGameStateChanged;

        // Debug initial state
        if (debug)
        {
            if (GameManager.Instance != null)
                Debug.Log($"Initial GameState: {GameManager.Instance.CurrentState}, IsMovementEnabled: {IsMovementEnabled}");
            else
                Debug.LogError("GameManager.Instance is null!");
            if (lightManager != null)
                Debug.Log($"Initial TimeOfDay: {lightManager.TimeOfDay}");
        }
    }

    void OnDestroy()
    {
        ActionManager.OnTrashCollected -= AddTrashPoints;
        GameManager.OnGameStateChanged -= HandleGameStateChanged;
    }

    private void HandleGameStateChanged(GameState newState)
    {
        IsMovementEnabled = newState == GameState.GamePlay;
        if (debug)
            Debug.Log($"Game state changed to: {newState}, IsMovementEnabled: {IsMovementEnabled}");
    }

    void Update()
    {
        if (playerData == null) return;

        // Regeneration in Shop state
        if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Shop)
        {
            // Regenerate HP (not time-restricted)
            hpRegenAccumulator += playerData.HpRegenRate * Time.deltaTime;
            int hpToAdd = (int)hpRegenAccumulator;
            if (hpToAdd > 0)
            {
                playerData.PlayerHP += hpToAdd;
                hpRegenAccumulator -= hpToAdd;
            }

            // Regenerate Energy (only during daytime: 350 ≤ TimeOfDay < 1100)
            if (lightManager != null && lightManager.TimeOfDay >= 350f && lightManager.TimeOfDay < 1100f)
            {
                energyRegenAccumulator += playerData.EnergyRegenRate * Time.deltaTime;
                int energyToAdd = (int)energyRegenAccumulator;
                if (energyToAdd > 0)
                {
                    playerData.PlayerEnergy += energyToAdd;
                    energyRegenAccumulator -= energyToAdd;
                }

                if (debug && (hpToAdd > 0 || energyToAdd > 0))
                {
                    Debug.Log($"Regenerated (Daytime): HP +{hpToAdd}, Energy +{energyToAdd}. Current: HP={playerData.PlayerHP}, Energy={playerData.PlayerEnergy}, TimeOfDay={lightManager.TimeOfDay}");
                }
            }
            else if (debug && hpToAdd > 0)
            {
                Debug.Log($"Regenerated (Nighttime): HP +{hpToAdd}, Energy +0 (not daytime). Current: HP={playerData.PlayerHP}, Energy={playerData.PlayerEnergy}, TimeOfDay={(lightManager != null ? lightManager.TimeOfDay.ToString() : "N/A")}");
            }
        }

        // Check for zero health or energy
        if (playerData.PlayerEnergy == 0 && !playerData.EmergencyReservesPowerActive)
        {
            playerData.EmergencyReservesPower();
            if (debug)
                Debug.Log("Energy reached 0. Triggered EmergencyReservesPower.");
        }

        if (playerData.PlayerHP == 0 && !playerData.EmergencyReservesHealthActive)
        {
            playerData.EmergencyReservesHealth();
            if (debug)
                Debug.Log("Health reached 0. Triggered EmergencyReservesHealth.");
        }

        // Process movement input
        if (IsMovementEnabled)
        {
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");
            moveInput = new Vector3(horizontal, 0f, vertical).normalized;

            if (debug && moveInput.magnitude > 0.1f)
                Debug.Log($"Input: {moveInput}, Magnitude: {moveInput.magnitude}");

            // Energy drain during movement
            if (moveInput.magnitude > 0.1f && GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.GamePlay)
            {
                energyDrainTimer += Time.deltaTime;
                if (energyDrainTimer >= energyDrainInterval)
                {
                    playerData.PlayerEnergy -= 1;
                    energyDrainTimer -= energyDrainInterval;
                    if (debug)
                        Debug.Log($"Drained 1 energy. Remaining energy: {playerData.PlayerEnergy}, Timer: {energyDrainTimer}");
                }
            }
            else
            {
                energyDrainTimer = 0f;
            }
        }
        else
        {
            moveInput = Vector3.zero;
            energyDrainTimer = 0f;
            if (debug)
                Debug.Log("Movement disabled: moveInput set to zero.");
        }

        if (storageFullCooldown > 0f)
            storageFullCooldown -= Time.deltaTime;

        HandleTrashInteraction();
    }

    void FixedUpdate()
    {
        if (IsMovementEnabled)
        {
            MovePlayer();
            if (rotateToMovement && moveInput.magnitude > 0.1f)
                RotatePlayer();
        }
        ApplyBuoyancyAndWaterMotion();
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

        float targetPitch, targetRoll;
        Vector3 currentEuler = rb.rotation.eulerAngles;
        float yaw = currentEuler.y;

        if (isCorrectingToNaturalAngle)
        {
            targetPitch = -5f;
            targetRoll = 0f;

            float currentPitch = NormalizeAngle(currentEuler.x);
            float currentRoll = NormalizeAngle(currentEuler.z);
            if (Mathf.Abs(currentPitch - targetPitch) < 0.1f && Mathf.Abs(currentRoll - targetRoll) < 0.1f)
            {
                isCorrectingToNaturalAngle = false;
                if (debug)
                    Debug.Log("Finished correcting to natural angle (-5, 0, 0)");
            }
        }
        else
        {
            targetRoll = Mathf.Sin(time * rollFrequency * 2f * Mathf.PI) * rollAmplitude;
            targetPitch = Mathf.Cos(time * pitchFrequency * 2f * Mathf.PI) * pitchAmplitude - 5f;
        }

        targetPitch = Mathf.Clamp(targetPitch, -25f, 25f);
        targetRoll = Mathf.Clamp(targetRoll, -20f, 20f);

        float normalizedPitch = NormalizeAngle(targetPitch);
        float normalizedRoll = NormalizeAngle(targetRoll);

        Quaternion targetRotation = Quaternion.Euler(normalizedPitch, yaw, normalizedRoll);
        rb.rotation = Quaternion.Slerp(rb.rotation, targetRotation, Time.fixedDeltaTime * (isCorrectingToNaturalAngle ? collisionCorrectionSpeed : 2f));

        if (debug)
            Debug.Log($"Current Rotation: ({currentEuler.x}, {yaw}, {currentEuler.z}), Target: ({normalizedPitch}, {yaw}, {normalizedRoll}), Correcting: {isCorrectingToNaturalAngle}");
    }

    private void RotatePlayer()
    {
        Vector3 moveDirection = CameraRelativeDirection(moveInput);
        Quaternion targetYaw = Quaternion.LookRotation(moveDirection, Vector3.up);
        Vector3 euler = targetYaw.eulerAngles;
        euler.x = rb.rotation.eulerAngles.x;
        euler.z = rb.rotation.eulerAngles.z;
        targetYaw = Quaternion.Euler(euler);
        rb.rotation = Quaternion.Slerp(rb.rotation, targetYaw, Time.fixedDeltaTime * rotationSpeed / 360f);
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
        if (trashInteractionSystem == null || trashInteractionSystem.ClosestTrash == null || playerData == null)
            return;

        // Block trash collection if health or energy is 0
        if (playerData.PlayerHP == 0 || playerData.PlayerEnergy == 0)
        {
            if (storageFullCooldown <= 0f)
            {
                trashInteractionSystem.CanvasInstance.ShowTempMessage("Cannot collect: Health or Energy depleted", 1f);
                storageFullCooldown = 1f;
                if (debug)
                    Debug.Log("Cannot collect trash: Health or Energy is 0");
            }
            return;
        }

        InteractableTrash closest = trashInteractionSystem.ClosestTrash;
        TrashProperties properties = closest.TrashProperties;

        if (properties == null)
            return;

        int points = properties.GetPoints();
        bool canCollect = playerData.CurrentTrash + points <= playerData.MaxPlayerStorage;

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
                storageFullCooldown = 1f;
                if (debug)
                    Debug.Log("Cannot collect surface trash: Storage full");
            }
        }
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
                        storageFullCooldown = 1f;
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

    private void OnCollisionEnter(Collision collision)
    {
        isCorrectingToNaturalAngle = true;
        if (debug)
            Debug.Log($"Collision detected with {collision.gameObject.name}. Correcting to natural angle (-5, 0, 0)");
    }

    private float NormalizeAngle(float angle)
    {
        while (angle > 180f) angle -= 360f;
        while (angle < -180f) angle += 360f;
        return angle;
    }
}