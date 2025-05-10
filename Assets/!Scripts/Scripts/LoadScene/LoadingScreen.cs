using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class LoadingScreen : MonoBehaviour
{
    [SerializeField, Tooltip("Slider to display loading progress")]
    private Slider progressSlider;

    [SerializeField, Tooltip("Icon image to spin during loading")]
    private Image spinningIcon;

    [SerializeField, Tooltip("Speed of icon rotation (degrees per second)")]
    private float rotationSpeed = 180f;

    [SerializeField, Tooltip("Fixed loading time in seconds")]
    private float loadingTime = 10f;

    [SerializeField, Tooltip("Enable debug logs for loading progress")]
    private bool debug = false;

    [SerializeField, Tooltip("Name of the scene to load")]
    private string targetSceneName = "GameScene";

    private AsyncOperation operation;
    private float timer;
    private bool isLoading;

    private void Start()
    {
        // Validate references
        if (progressSlider == null)
            Debug.LogError("Progress Slider not assigned in LoadingScreen!");
        if (spinningIcon == null)
            Debug.LogError("Spinning Icon not assigned in LoadingScreen!");
        if (string.IsNullOrEmpty(targetSceneName))
            Debug.LogError("Target Scene Name not set in LoadingScreen!");

        // Initialize the slider
        if (progressSlider != null)
        {
            progressSlider.minValue = 0f;
            progressSlider.maxValue = 1f;
            progressSlider.value = 0f;
        }

        // Unload unused assets to free memory
        Resources.UnloadUnusedAssets();

        // Begin preloading the scene
        if (!isLoading)
        {
            isLoading = true;
            StartCoroutine(PreloadSceneWithDelay());
        }
    }

    private void Update()
    {
        // Update the progress slider based on elapsed time
        if (progressSlider != null)
        {
            float progress = Mathf.Clamp01(timer / loadingTime);
            progressSlider.value = progress;

            if (debug)
                Debug.Log($"LoadingScreen: Time Progress = {progress * 100:F1}%, Scene Load Progress = {(operation != null ? operation.progress * 100 / 0.9f : 0):F1}%");
        }

        // Rotate the spinning icon
        if (spinningIcon != null)
        {
            spinningIcon.rectTransform.Rotate(0f, 0f, -rotationSpeed * Time.deltaTime);
        }
    }

    private IEnumerator PreloadSceneWithDelay()
    {
        // Validate scene exists in Build Settings
        if (!IsSceneInBuildSettings(targetSceneName))
        {
            Debug.LogError($"Target scene '{targetSceneName}' not found in Build Settings! LoadingScreen will not proceed.");
            yield break;
        }

        // Start preloading the scene asynchronously
        operation = SceneManager.LoadSceneAsync(targetSceneName);
        if (operation == null)
        {
            Debug.LogError($"Failed to start loading scene '{targetSceneName}'. Check scene setup.");
            isLoading = false;
            yield break;
        }

        operation.allowSceneActivation = false;

        // Track elapsed time
        timer = 0f;
        while (timer < loadingTime)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        // Allow the scene to activate
        operation.allowSceneActivation = true;

        // Wait until the scene is fully loaded and activated
        while (!operation.isDone)
        {
            if (debug)
                Debug.Log($"LoadingScreen: Waiting for scene activation, progress = {operation.progress * 100 / 0.9f:F1}%");
            yield return null;
        }

        isLoading = false;
        if (debug)
            Debug.Log($"LoadingScreen: Scene '{targetSceneName}' fully loaded and activated");
    }

    private bool IsSceneInBuildSettings(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            string name = System.IO.Path.GetFileNameWithoutExtension(path);
            if (name == sceneName)
                return true;
        }
        return false;
    }
}