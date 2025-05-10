using UnityEngine;

public class MaxEnginePower : MonoBehaviour
{
    [SerializeField] private GameObject boat1;
    [SerializeField] private GameObject boat2;
    void OnEnable()
    {
        ActionManager.OnMaxEnginePowerUpgrade += OnMaxEnginePower;    
    }

    void OnDisable()
    {
        ActionManager.OnMaxEnginePowerUpgrade -= OnMaxEnginePower;    
    }

    private void OnMaxEnginePower()
    {
        boat1.SetActive(false);
        boat2.SetActive(true);
    }
}
