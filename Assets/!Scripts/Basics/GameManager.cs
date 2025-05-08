using UnityEngine;
using System;

// Enum for game states
public enum GameState
{
    GamePlay,
    Paused,
    Shop
}

public class GameManager : MonoBehaviour
{
    // Singleton instance
    public static GameManager Instance { get; private set; }

    // Current and previous game states
    public GameState CurrentState { get; private set; }
    private GameState previousState;

    // Events for state changes
    public static event Action<GameState> OnGameStateChanged;

    [SerializeField] private GameObject shopCanvas;

    private void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Initialize state
        CurrentState = GameState.GamePlay;
        previousState = GameState.GamePlay;
    }

    private void Start()
    {
        // Lock cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void ToggleShopCanvas()
    {
        if (CurrentState == GameState.Shop)
        {
            shopCanvas.SetActive(false);
        }
        else
        {
            shopCanvas.SetActive(true);
        }
    }

    // Switch to a new game state
    public void SetGameState(GameState newState)
    {
        if (newState == CurrentState) return;

        previousState = CurrentState;
        CurrentState = newState;

        // Handle state-specific logic
        switch (newState)
        {
            case GameState.GamePlay:
                Time.timeScale = 1f;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                break;
            case GameState.Paused:
                Time.timeScale = 0f;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                break;
            case GameState.Shop:
                Time.timeScale = 1f;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                break;
        }

        Debug.Log($"Game state changed to: {newState}, Previous state: {previousState}");
    }

    // Revert to the previous game state
    public void RevertToPreviousState()
    {
        if (previousState != CurrentState)
        {
            SetGameState(previousState);
        }
    }
}