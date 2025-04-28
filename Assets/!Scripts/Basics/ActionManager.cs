using UnityEngine;
using System;
using System.Diagnostics;
using Unity.VisualScripting;

public static class ActionManager
{
    public static event Action OnPaused;
    public static event Action OnUnpaused;

    public static event Action<string> OnEndGame;

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

}
