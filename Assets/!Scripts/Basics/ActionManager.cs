using UnityEngine;
using System;

public static class ActionManager
{
    public static event Action OnPaused;
    public static event Action OnUnpaused;
    public static event Action<string> OnEndGame;
    public static event Action<int> OnTrashCollected;

    public static event Action OnDockingComplete;
    public static event Action OnDockingIncomplete;
    public static event Action OnDockingInProgress;

    public static void InvokeDockingComplete()
    {
        OnDockingComplete?.Invoke();
    }

    public static void InvokeDockingIncomplete()
    {
        OnDockingIncomplete?.Invoke();
    }

    public static void InvokeDockingInProgress()
    {
        OnDockingInProgress?.Invoke();
    }

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