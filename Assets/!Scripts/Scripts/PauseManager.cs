using UnityEditor.SearchService;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    [SerializeField, Tooltip("The pause canvas to show when paused")]
    private GameObject pauseCanvas;

    [SerializeField, Tooltip("The player GUI canvas to hide when paused")]
    private GameObject playerGUICanvas;

    [SerializeField, Tooltip("Enable debug logs for pause/resume actions")]
    private bool debug = false;

    private void Start()
    {
        // Validate references
        if (pauseCanvas == null)
            Debug.LogError("Pause Canvas not assigned in PauseManager!");
        if (playerGUICanvas == null)
            Debug.LogError("Player GUI Canvas not assigned in PauseManager!");
        if (GameManager.Instance == null)
            Debug.LogError("GameManager instance not found in PauseManager!");

        // Initialize canvas states
        if (pauseCanvas != null)
            pauseCanvas.SetActive(false);
        if (playerGUICanvas != null)
            playerGUICanvas.SetActive(true);
    }

    private void Update()
    {
        // Check for Escape key press
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // Handle pause/resume based on current state
            switch (GameManager.Instance.CurrentState)
            {
                case GameState.GamePlay:
                    PauseGame();
                    break;
                case GameState.Paused:
                    ResumeGame();
                    break;
                case GameState.Shop:
                    // Do nothing, or optionally revert to previous state
                    if (debug)
                        Debug.Log("Escape pressed in Shop state; no pause action taken.");
                    break;
            }
        }
    }

    public void PauseGame()
    {
        if (GameManager.Instance == null)
            return;

        // Set game state to Paused
        GameManager.Instance.SetGameState(GameState.Paused);

        // Show pause canvas and hide player GUI
        if (pauseCanvas != null)
            pauseCanvas.SetActive(true);
        if (playerGUICanvas != null)
            playerGUICanvas.SetActive(false);

        if (debug)
            Debug.Log("Game paused: Pause canvas shown, Player GUI hidden.");
    }

    public void ResumeGame()
    {
        if (GameManager.Instance == null)
            return;

        // Revert to GamePlay state
        GameManager.Instance.SetGameState(GameState.GamePlay);

        // Hide pause canvas and show player GUI
        if (pauseCanvas != null)
            pauseCanvas.SetActive(false);
        if (playerGUICanvas != null)
            playerGUICanvas.SetActive(true);

        if (debug)
            Debug.Log("Game resumed: Pause canvas hidden, Player GUI shown.");
    }

    public void RestartGame()
    {
        // Reset game state if necessary
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetGameState(GameState.GamePlay);
            if (debug)
                Debug.Log("RestartManager: Game state set to GamePlay before restart.");
        }

        // Reset Time.timeScale in case it was paused
        Time.timeScale = 1f;

        // Unload unused assets to prevent memory issues
        Resources.UnloadUnusedAssets();

        // Load the Loading scene
        SceneManager.LoadScene("Loading");

        if (debug)
            Debug.Log("RestartManager: Loading scene initiated for restart.");
    }

    public void QuitGame()
    {
        // Quit the application
        Application.Quit();

        // Log to console (only visible in editor)
        Debug.Log("Game is quitting...");
    }
}