using UnityEngine;
using System.Collections.Generic;

public class NetUpgrade : MonoBehaviour
{
    [SerializeField, Tooltip("Net prefab GameObjects to activate on upgrade")]
    private GameObject[] netPrefab;

    [SerializeField, Tooltip("Player data ScriptableObject for net unlock status")]
    private PlayerData playerData;

    [SerializeField, Tooltip("Transform on Boat 1 for joint attachment (rear of boat)")]
    private Transform boat1JointAnchor;

    [SerializeField, Tooltip("Transform on Boat 2 for joint attachment (rear of boat)")]
    private Transform boat2JointAnchor;

    [SerializeField, Tooltip("Tag for Boat 1 prefab")]
    private string boat1Tag = "Boat1";

    [SerializeField, Tooltip("Tag for Boat 2 prefab")]
    private string boat2Tag = "Boat2";

    [SerializeField, Tooltip("LineRenderer prefab for visual connection (with material)")]
    private LineRenderer lineRendererPrefab;

    [SerializeField, Tooltip("Number of points in the LineRenderer for smooth curve")]
    private int linePoints = 20;

    [SerializeField, Tooltip("Height of the curve’s midpoint (meters above straight line)")]
    private float curveHeight = 0.5f;

    [SerializeField, Tooltip("Enable debug logs for net and joint behavior")]
    private bool debug = true;

    [SerializeField, Tooltip("Mass of the net’s Rigidbody (adjust for responsiveness)")]
    private float netMass = 20f;

    [SerializeField, Tooltip("Linear drag of the net’s Rigidbody (adjust for damping)")]
    private float netDrag = 2f;

    [SerializeField, Tooltip("Angular drag of the net’s Rigidbody (prevents spinning)")]
    private float netAngularDrag = 5f;

    [SerializeField, Tooltip("Spring strength for the joint (higher for tighter tug)")]
    private float jointSpring = 500f;

    [SerializeField, Tooltip("Damper for the joint (higher reduces oscillations)")]
    private float jointDamper = 50f;

    [SerializeField, Tooltip("Maximum distance the net can stretch from the boat (meters)")]
    private float jointLinearLimit = 1f;

    [SerializeField, Tooltip("Initial spawn offset for net behind boat (meters)")]
    private Vector3 netSpawnOffset = new Vector3(0f, 0f, -1f);

    [SerializeField, Tooltip("Minimum Y position for the net (world space)")]
    private float minYPosition = 1.5f;

    [Header("Net Storage")]
    [SerializeField, Tooltip("Maximum storage capacity of the net")]
    private int netStorageCapacity = 100;

    [SerializeField, Tooltip("Prefabs to indicate net fullness (e.g., 100 prefabs for 100% capacity)")]
    private GameObject[] fullnessIndicatorPrefabs;

    private bool isNetActive = false;
    private GameObject activeNet;
    private ConfigurableJoint netJoint;
    private LineRenderer lineRenderer;
    private Transform activeBoatAnchor;
    private int currentNetStorage = 0;

    private void OnEnable()
    {
        ActionManager.OnTrashNetUpgrade += OnTrashNetUpgrade;
    }

    private void OnDisable()
    {
        ActionManager.OnTrashNetUpgrade -= OnTrashNetUpgrade;
    }

    private void Start()
    {
        // Validate references
        if (playerData == null)
            Debug.LogError("PlayerData not assigned in NetUpgrade!");
        if (netPrefab == null || netPrefab.Length == 0)
            Debug.LogError("NetPrefab array not assigned or empty in NetUpgrade!");
        if (boat1JointAnchor == null || boat2JointAnchor == null)
            Debug.LogError("Boat joint anchors (Boat1 or Boat2) not assigned in NetUpgrade!");
        if (lineRendererPrefab == null)
            Debug.LogError("LineRenderer prefab not assigned in NetUpgrade!");
        if (fullnessIndicatorPrefabs == null || fullnessIndicatorPrefabs.Length == 0)
            Debug.LogError("Fullness indicator prefabs not assigned in NetUpgrade!");

        // Ensure net and indicator prefabs are initially inactive
        foreach (GameObject net in netPrefab)
        {
            if (net != null)
                net.SetActive(false);
            else
                Debug.LogWarning("Null net prefab found in NetUpgrade!");
        }
        foreach (GameObject indicator in fullnessIndicatorPrefabs)
        {
            if (indicator != null)
                indicator.SetActive(false);
            else
                Debug.LogWarning("Null fullness indicator prefab found in NetUpgrade!");
        }

        // Check if net is already unlocked
        if (playerData != null && playerData.TrashNetUnlocked)
        {
            OnTrashNetUpgrade();
        }
    }

    private void FixedUpdate()
    {
        if (!isNetActive || activeNet == null || netJoint == null)
            return;

        // Clamp net's Y position to minYPosition
        Rigidbody netRb = activeNet.GetComponent<Rigidbody>();
        if (netRb != null)
        {
            Vector3 currentPosition = netRb.position;
            if (currentPosition.y < minYPosition)
            {
                currentPosition.y = minYPosition;
                netRb.MovePosition(currentPosition);
                if (debug)
                    Debug.Log($"Net Y clamped to {minYPosition}. Current position: {currentPosition}");
            }
        }
    }

    private void Update()
    {
        if (!isNetActive || activeNet == null || netJoint == null)
            return;

        // Determine active boat
        UpdateActiveBoat();

        // Update LineRenderer
        if (lineRenderer != null && activeBoatAnchor != null)
        {
            UpdateLineRenderer();
        }

        // Debug net’s state
        if (debug && activeNet != null && activeBoatAnchor != null)
        {
            Rigidbody netRb = activeNet.GetComponent<Rigidbody>();
            if (netRb != null)
            {
                float distance = Vector3.Distance(activeBoatAnchor.position, netRb.position);
                bool isKinematic = netRb.isKinematic;
                bool hasConstraints = netRb.constraints != RigidbodyConstraints.None;
                Debug.Log($"Net distance: {distance:F2}m, Y: {netRb.position.y:F2}, Velocity: {netRb.linearVelocity.magnitude:F2} m/s, " +
                          $"IsKinematic: {isKinematic}, Constraints: {netRb.constraints}, " +
                          $"ConnectedBody: {(netJoint.connectedBody != null ? netJoint.connectedBody.name : "None")}, " +
                          $"Storage: {currentNetStorage}/{netStorageCapacity}");
            }
        }
    }

    private void OnTrashNetUpgrade()
    {
        // Activate the first valid net prefab
        foreach (GameObject net in netPrefab)
        {
            if (net != null)
            {
                net.SetActive(true);
                activeNet = net;
                break;
            }
        }

        if (activeNet == null)
        {
            Debug.LogError("No valid net prefab found in NetUpgrade!");
            return;
        }

        // Position net near active boat
        UpdateActiveBoat();
        if (activeBoatAnchor != null)
        {
            Vector3 spawnPosition = activeBoatAnchor.position + activeBoatAnchor.TransformDirection(netSpawnOffset);
            if (spawnPosition.y < minYPosition)
            {
                spawnPosition.y = minYPosition;
                if (debug)
                    Debug.Log($"Net spawn Y adjusted to {minYPosition}. Spawn position: {spawnPosition}");
            }
            activeNet.transform.position = spawnPosition;
            activeNet.transform.rotation = activeBoatAnchor.rotation;
        }
        else
        {
            Debug.LogWarning("No active boat anchor found during net activation. Net may not position correctly.");
        }

        // Setup joint on net
        SetupNetJoint();

        // Instantiate LineRenderer
        SetupLineRenderer();

        // Enable net functionality
        isNetActive = true;

        if (debug)
            Debug.Log($"Net upgrade activated: Dragging net with joint (Mass: {netMass}, Drag: {netDrag}, " +
                      $"Spring: {jointSpring}, Damper: {jointDamper}, Limit: {jointLinearLimit}m, " +
                      $"SpawnOffset: {netSpawnOffset}, MinY: {minYPosition}, Storage: {netStorageCapacity}).");
    }

    private void SetupNetJoint()
    {
        // Ensure net has a Rigidbody
        Rigidbody netRb = activeNet.GetComponent<Rigidbody>();
        if (netRb == null)
        {
            netRb = activeNet.AddComponent<Rigidbody>();
        }

        // Configure Rigidbody
        netRb.mass = netMass;
        netRb.linearDamping = netDrag;
        netRb.angularDamping = netAngularDrag;
        netRb.useGravity = true;
        netRb.isKinematic = false;
        netRb.constraints = RigidbodyConstraints.None;
        netRb.interpolation = RigidbodyInterpolation.Interpolate;

        // Add or configure ConfigurableJoint
        netJoint = activeNet.GetComponent<ConfigurableJoint>();
        if (netJoint == null)
        {
            netJoint = activeNet.AddComponent<ConfigurableJoint>();
            if (netJoint == null)
            {
                Debug.LogError($"Failed to add ConfigurableJoint to {activeNet.name}! Ensure GameObject is active and valid.");
                return;
            }
        }

        // Configure joint
        netJoint.autoConfigureConnectedAnchor = false;
        netJoint.anchor = Vector3.zero;
        netJoint.connectedAnchor = Vector3.zero;
        netJoint.xMotion = ConfigurableJointMotion.Limited;
        netJoint.yMotion = ConfigurableJointMotion.Limited;
        netJoint.zMotion = ConfigurableJointMotion.Limited;
        netJoint.angularXMotion = ConfigurableJointMotion.Limited;
        netJoint.angularYMotion = ConfigurableJointMotion.Limited;
        netJoint.angularZMotion = ConfigurableJointMotion.Limited;

        // Set soft limits for tight dragging
        SoftJointLimit linearLimit = new SoftJointLimit { limit = jointLinearLimit, bounciness = 0f, contactDistance = 0.1f };
        netJoint.linearLimit = linearLimit;

        SoftJointLimitSpring linearSpring = new SoftJointLimitSpring { spring = jointSpring, damper = jointDamper };
        netJoint.linearLimitSpring = linearSpring;

        // Limit angular motion to prevent flipping
        SoftJointLimit angularLimit = new SoftJointLimit { limit = 30f, bounciness = 0f, contactDistance = 0.1f };
        netJoint.lowAngularXLimit = angularLimit;
        netJoint.highAngularXLimit = angularLimit;
        netJoint.angularYLimit = angularLimit;
        netJoint.angularZLimit = angularLimit;

        // Ensure joint is connected
        UpdateActiveBoat();
        if (activeBoatAnchor != null)
        {
            Rigidbody boatRb = activeBoatAnchor.GetComponent<Rigidbody>();
            if (boatRb != null)
            {
                netJoint.connectedBody = boatRb;
                netJoint.connectedAnchor = activeBoatAnchor.localPosition;
                if (debug)
                    Debug.Log($"Joint connected to {boatRb.name}, Anchor: {netJoint.connectedAnchor}");
            }
            else
            {
                Debug.LogError($"Active boat anchor ({activeBoatAnchor.name}) has no Rigidbody!");
            }
        }
        else
        {
            Debug.LogError("No active boat anchor found! Net joint not connected.");
        }
    }

    private void UpdateActiveBoat()
    {
        // Find active boat by tag and activity
        GameObject boat1 = GameObject.FindGameObjectWithTag(boat1Tag);
        GameObject boat2 = GameObject.FindGameObjectWithTag(boat2Tag);

        if (boat1 != null && boat1.activeInHierarchy)
        {
            activeBoatAnchor = boat1JointAnchor;
            if (debug && activeBoatAnchor != netJoint?.connectedBody?.transform)
                Debug.Log($"Switching net joint to Boat 1 (Tag: {boat1Tag}).");
        }
        else if (boat2 != null && boat2.activeInHierarchy)
        {
            activeBoatAnchor = boat2JointAnchor;
            if (debug && activeBoatAnchor != netJoint?.connectedBody?.transform)
                Debug.Log($"Switching net joint to Boat 2 (Tag: {boat2Tag}).");
        }
        else
        {
            activeBoatAnchor = null;
            if (debug)
                Debug.LogWarning("No active boat found (Boat1 and Boat2 inactive). Net may not move.");
        }
    }

    private void SetupLineRenderer()
    {
        // Instantiate LineRenderer from prefab
        lineRenderer = Instantiate(lineRendererPrefab, transform);
        lineRenderer.positionCount = linePoints;
        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;
    }

    private void UpdateLineRenderer()
    {
        if (activeBoatAnchor == null || activeNet == null)
            return;

        // Get start and end points
        Vector3 start = activeBoatAnchor.position;
        Vector3 end = activeNet.transform.position;

        // Calculate control point for quadratic Bezier curve
        Vector3 midPoint = (start + end) / 2f;
        midPoint += Vector3.up * curveHeight;

        // Generate Bezier curve points
        for (int i = 0; i < linePoints; i++)
        {
            float t = i / (float)(linePoints - 1);
            Vector3 point = CalculateQuadraticBezierPoint(t, start, midPoint, end);
            lineRenderer.SetPosition(i, point);
        }
    }

    private Vector3 CalculateQuadraticBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2)
    {
        float u = 1f - t;
        float tt = t * t;
        float uu = u * u;

        Vector3 point = (uu * p0) + (2f * u * t * p1) + (tt * p2);
        return point;
    }

public void HandleNetCollision(Collision collision)
{
    if (!isNetActive || collision.gameObject.layer != LayerMask.NameToLayer("Trash"))
        return;

    TrashProperties trash = collision.gameObject.GetComponent<TrashProperties>();
    if (trash != null)
    {
        playerData.TrashNetUnlocked = true;
        int points = trash.GetPoints();
        if (currentNetStorage + points <= netStorageCapacity)
        {
            currentNetStorage += points;
            UpdateFullnessIndicators();
            Destroy(collision.gameObject);
            if (debug)
                Debug.Log($"Collected {points} points (TrashClass: {trash.TrashClass}). Storage: {currentNetStorage}/{netStorageCapacity}");
        }
        else if (debug)
        {
            Debug.Log($"Net is full! Cannot collect {points} points from {trash.TrashClass}.");
        }
    }
    else if (debug)
    {
        Debug.LogWarning($"No TrashProperties found on {collision.gameObject.name}. Collision ignored.");
    }
}
    private void UpdateFullnessIndicators()
    {
        float fillPercentage = (float)currentNetStorage / netStorageCapacity;
        int activeCount = Mathf.CeilToInt(fillPercentage * fullnessIndicatorPrefabs.Length);
        for (int i = 0; i < fullnessIndicatorPrefabs.Length; i++)
        {
            if (fullnessIndicatorPrefabs[i] != null)
                fullnessIndicatorPrefabs[i].SetActive(i < activeCount);
        }
        if (debug)
            Debug.Log($"Net fullness updated: {fillPercentage * 100:F1}% ({activeCount}/{fullnessIndicatorPrefabs.Length} prefabs active)");
    }

    public int GetCurrentNetStorage() => currentNetStorage;

    public void ResetNet()
    {
        currentNetStorage = 0;
        UpdateFullnessIndicators();
        if (debug)
            Debug.Log("Net storage reset to 0.");
    }

    private void OnDrawGizmos()
    {
        if (boat1JointAnchor != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(boat1JointAnchor.position, 0.2f);
        }
        if (boat2JointAnchor != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(boat2JointAnchor.position, 0.2f);
        }
    }
}