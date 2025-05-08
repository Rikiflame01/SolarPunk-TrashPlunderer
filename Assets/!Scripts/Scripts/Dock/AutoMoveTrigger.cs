using UnityEngine;
using TMPro;
using System.Collections;

public class AutoMoveTrigger : MonoBehaviour
{
    [SerializeField] private PlayerData playerData;

    [SerializeField, Tooltip("Target transform to move the player to when inside the trigger")]
    private Transform targetTransform;

    [SerializeField, Tooltip("Transform to move the player to when exiting (on Enter key)")]
    private Transform exitTransform;

    [SerializeField, Tooltip("Speed of movement towards the target or exit (meters per second)")]
    private float moveSpeed = 5f;

    [SerializeField, Tooltip("Communication canvas GameObject with TextMeshProUGUI for Docking text")]
    private GameObject communicationCanvas;

    [SerializeField, Tooltip("TextMeshProUGUI component for displaying 'Docking...' or 'UnDocking...'")]
    private TextMeshProUGUI dockingText;

    [SerializeField, Tooltip("Time interval for dot animation cycle (seconds)")]
    private float dotAnimationInterval = 0.5f;

    [SerializeField, Tooltip("Enable debug logs")]
    private bool debug = true;

    private PlayerController playerController;
    private Rigidbody playerRigidbody;
    private bool isPlayerInside;
    private bool hasReachedTarget;
    private bool isMovingToExit;
    private bool canTriggerExit;
    private bool canDock = true; // Controls cooldown
    private Vector3 destination;
    private float dotTimer;
    private int dotCount;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isMovingToExit && canDock)
        {
            playerController = other.GetComponent<PlayerController>();
            playerRigidbody = other.GetComponent<Rigidbody>();

            if (playerController == null || playerRigidbody == null)
            {
                Debug.LogError("Player lacks PlayerController or Rigidbody!");
                return;
            }

            if (targetTransform == null)
            {
                Debug.LogError("Target Transform is not assigned!");
                return;
            }

            isPlayerInside = true;
            hasReachedTarget = false;
            isMovingToExit = false;
            canTriggerExit = false;
            destination = targetTransform.position;
            dotTimer = 0f;
            dotCount = 1;

            playerController.IsMovementEnabled = false;
            playerRigidbody.constraints = RigidbodyConstraints.None;

            if (communicationCanvas != null && dockingText != null)
            {
                communicationCanvas.SetActive(true);
                UpdateDockingText();
                ActionManager.InvokeDockingInProgress();
                if (debug)
                    Debug.Log($"CommunicationCanvas enabled for Docking text: {communicationCanvas.activeSelf}");
            }

            if (debug)
                Debug.Log($"Player entered trigger. Moving to target: {destination}");
        }
        else if (other.CompareTag("Player"))
        {
            if (debug)
                Debug.Log($"OnTriggerEnter ignored: Conditions not met (isMovingToExit: {isMovingToExit}, canDock: {canDock})");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && !isMovingToExit)
        {
            ReleasePlayerControl(false);
            if (debug)
                Debug.Log("Player exited trigger naturally.");
        }
    }

    public void OnDestinationTriggerEnter(Collider other, string triggerTag)
    {
        if (other.CompareTag("Player") && isPlayerInside && playerRigidbody != null)
        {
            if (triggerTag == "TargetTrigger" && !isMovingToExit)
            {
                GameManager.Instance.ToggleShopCanvas();
                GameManager.Instance.SetGameState(GameState.Shop);
                hasReachedTarget = true;
                canTriggerExit = true;
                playerRigidbody.linearVelocity = Vector3.zero;

                if (communicationCanvas != null)
                {
                    communicationCanvas.SetActive(false);
                    if (debug)
                        Debug.Log($"CommunicationCanvas disabled for target trigger: {communicationCanvas.activeSelf}");
                }

                ActionManager.InvokeDockingComplete();
                if (debug)
                    Debug.Log("Player reached target trigger. Invoked DockingComplete. Exit trigger now eligible.");
            }
            else if (triggerTag == "ExitTrigger" && isMovingToExit && hasReachedTarget && canTriggerExit)
            {
                playerRigidbody.linearVelocity = Vector3.zero;

                if (communicationCanvas != null)
                {
                    communicationCanvas.SetActive(false);
                    if (debug)
                        Debug.Log($"CommunicationCanvas disabled for exit trigger: {communicationCanvas.activeSelf}");
                }

                playerData.LoadSavedStats();
                
                canTriggerExit = false;
                ActionManager.InvokeDockingComplete();
                playerController.IsMovementEnabled = true; // Re-enable player movement immediately
                GameManager.Instance.ToggleShopCanvas();
                GameManager.Instance.SetGameState(GameState.GamePlay);
                StartCoroutine(CompleteAndCooldown()); // Start cooldown process
                if (debug)
                    Debug.Log("Player reached exit trigger. Invoked DockingComplete and started cooldown.");
            }
            else if (triggerTag == "ExitTrigger")
            {
                if (debug)
                    Debug.Log($"Exit trigger ignored: Conditions not met (isMovingToExit: {isMovingToExit}, hasReachedTarget: {hasReachedTarget}, canTriggerExit: {canTriggerExit})");
            }
        }
    }

    private void Update()
    {
        if (isPlayerInside && playerController != null && Input.GetKeyDown(KeyCode.Return) && hasReachedTarget && canTriggerExit)
        {
            StartMovingToExit();
        }

        if (isPlayerInside && communicationCanvas != null && communicationCanvas.activeSelf && dockingText != null)
        {
            dotTimer += Time.deltaTime;
            if (dotTimer >= dotAnimationInterval)
            {
                dotCount = (dotCount % 3) + 1;
                dotTimer = 0f;
                UpdateDockingText();
            }
        }
    }

    private void FixedUpdate()
    {
        if (isPlayerInside && playerRigidbody != null && (!hasReachedTarget || isMovingToExit))
        {
            MoveToDestination();
        }
    }

    private void MoveToDestination()
    {
        if (hasReachedTarget && !isMovingToExit)
            return;

        Vector3 direction = (destination - playerRigidbody.position).normalized;
        Vector3 targetVelocity = direction * moveSpeed;
        playerRigidbody.linearVelocity = new Vector3(
            targetVelocity.x,
            playerRigidbody.linearVelocity.y,
            targetVelocity.z
        );

        if (debug)
            Debug.Log($"Moving to {(isMovingToExit ? "exit" : "target")}. Velocity: {playerRigidbody.linearVelocity}, Destination: {destination}");
    }

    private void StartMovingToExit()
    {
        if (exitTransform == null || playerRigidbody == null)
        {
            Debug.LogError("Exit transform or player Rigidbody not assigned!");
            return;
        }

        isMovingToExit = true;
        destination = exitTransform.position;
        dotTimer = 0f;
        dotCount = 1;

        if (communicationCanvas != null && dockingText != null)
        {
            communicationCanvas.SetActive(true);
            UpdateDockingText();
            ActionManager.InvokeDockingInProgress();
            if (debug)
                Debug.Log($"CommunicationCanvas enabled for UnDocking text: {communicationCanvas.activeSelf}");
        }

        if (debug)
            Debug.Log("Started moving to exit. Invoked DockingInProgress.");
    }

    private void UpdateDockingText()
    {
        if (dockingText == null)
            return;

        string baseText = isMovingToExit ? "UnDocking" : "Docking";
        string dots = new string('.', dotCount);
        dockingText.text = $"{baseText}{dots}";

        if (debug)
            Debug.Log($"Updated text: {dockingText.text}");
    }

    private void ReleasePlayerControl(bool dockingCompleted)
    {
        if (playerRigidbody != null)
        {
            playerRigidbody.constraints = RigidbodyConstraints.FreezeRotationY;
            playerRigidbody.linearVelocity = Vector3.zero;
        }

        if (playerController != null)
        {
            playerController.IsMovementEnabled = true;
        }

        if (communicationCanvas != null)
        {
            communicationCanvas.SetActive(false);
            if (debug)
                Debug.Log($"CommunicationCanvas disabled in ReleasePlayerControl: {communicationCanvas.activeSelf}");
        }

        if (!dockingCompleted)
        {
            ActionManager.InvokeDockingIncomplete();
            if (debug)
                Debug.Log("Player exited naturally. Invoked DockingIncomplete.");
        }

        // Reset state
        isPlayerInside = false;
        isMovingToExit = false;
        hasReachedTarget = false;
        canTriggerExit = false;
        playerController = null;
        playerRigidbody = null;

        if (debug)
            Debug.Log("State reset for re-docking.");
    }

    private IEnumerator CompleteAndCooldown()
    {
        yield return new WaitForSeconds(1f); // Wait for "Complete" display (assumed 1 second)
        ReleasePlayerControl(true); // Reset state after display
        canDock = false; // Prevent docking during cooldown
        yield return new WaitForSeconds(5f); // 5-second cooldown
        canDock = true; // Allow docking again
        if (debug)
            Debug.Log("Cooldown complete. Ready for next docking.");
    }

    private void OnValidate()
    {
        Collider collider = GetComponent<Collider>();
        if (collider == null)
        {
            Debug.LogError("AutoMoveTrigger requires a Collider component!");
        }
        else if (!collider.isTrigger)
        {
            Debug.LogWarning("Collider is not set as a trigger. Setting it now.");
            collider.isTrigger = true;
        }

        if (communicationCanvas != null && dockingText == null)
        {
            dockingText = communicationCanvas.GetComponentInChildren<TextMeshProUGUI>();
            if (dockingText == null)
                Debug.LogWarning("Communication canvas lacks a TextMeshProUGUI component!");
        }
    }
}