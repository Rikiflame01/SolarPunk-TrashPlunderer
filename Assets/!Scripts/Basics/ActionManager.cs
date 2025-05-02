using UnityEngine;
using System;

public static class ActionManager
{
    public static event Action OnPaused;
    public static event Action OnUnpaused;
    public static event Action<string> OnEndGame;
    public static event Action<int> OnTrashCollected; // New event for trash collection

    public static void InvokeEndGame(string player)
    {
        OnEndGame?.Invoke(player);
    }

    public static void InvokePaused()
    {
        OnPaused?.Invoke();
    }

    public static void InvokeUnpaused()
    {
        OnUnpaused?.Invoke();
    }

    public static void InvokeTrashCollected(int points)
    {
        OnTrashCollected?.Invoke(points);
    }
}