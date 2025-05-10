using UnityEngine;
using TMPro;

public class InteractionCanvas : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI promptText;
    private Transform cameraTransform;
    private string originalPrompt;
    private float messageTimer;
    private bool showingTempMessage;
    private Canvas canvas;

    public bool IsShowingTempMessage => showingTempMessage; // Public accessor

    public void Initialize(Transform cameraTransform)
    {
        this.cameraTransform = cameraTransform;
        canvas = GetComponent<Canvas>();
        if (canvas == null)
            Debug.LogError("No Canvas component on InteractionCanvas!");
        DisableCanvas();
    }

    public void ShowPrompt(string prompt, Vector3 position)
    {
        if (showingTempMessage)
            return;

        originalPrompt = prompt;
        promptText.text = prompt;
        transform.position = position;
        showingTempMessage = false;
        messageTimer = 0f;
        EnableCanvas();
    }

    public void ShowTempMessage(string message, float duration)
    {
        promptText.text = message;
        showingTempMessage = true;
        messageTimer = duration;
        EnableCanvas();
        Debug.Log($"Showing temp message: {message} for {duration}s");
    }

    public void HidePrompt()
    {
        DisableCanvas();
        showingTempMessage = false;
        messageTimer = 0f;
        Debug.Log("Hiding prompt");
    }

    private void EnableCanvas()
    {
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);
        if (canvas != null)
            canvas.enabled = true;
    }

    private void DisableCanvas()
    {
        if (gameObject.activeSelf)
            gameObject.SetActive(false);
        if (canvas != null)
            canvas.enabled = false;
    }

    void Update()
    {
        if (showingTempMessage)
        {
            messageTimer -= Time.deltaTime;
            Debug.Log($"Temp message timer: {messageTimer}s, current text: {promptText.text}");
            if (messageTimer <= 0f)
            {
                showingTempMessage = false;
                promptText.text = originalPrompt;
                Debug.Log($"Reverted to original prompt: {originalPrompt}");
            }
        }
    }

    void LateUpdate()
    {
        if (cameraTransform != null && gameObject.activeSelf)
        {
            Vector3 directionToCamera = cameraTransform.position - transform.position;
            directionToCamera.y = 0;
            if (directionToCamera.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.LookRotation(-directionToCamera);
            }
        }
    }
}