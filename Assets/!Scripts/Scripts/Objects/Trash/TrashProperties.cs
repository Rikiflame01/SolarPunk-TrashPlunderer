using UnityEngine;

public enum TrashClass
{
    Light,
    Medium,
    Heavy,
    SuperHeavy
}

public class TrashProperties : MonoBehaviour
{
    [SerializeField, Tooltip("Class of the trash")]
    private TrashClass trashClass = TrashClass.Light;

    // Get points based on trash class
    public int GetPoints()
    {
        switch (trashClass)
        {
            case TrashClass.Light: return 1;
            case TrashClass.Medium: return 2;
            case TrashClass.Heavy: return 5;
            case TrashClass.SuperHeavy: return 10;
            default: return 1;
        }
    }

    // Get hold time for UnderWaterTrash (in seconds)
    public float GetHoldTime()
    {
        switch (trashClass)
        {
            case TrashClass.Light: return 0f;
            case TrashClass.Medium: return 2f;
            case TrashClass.Heavy: return 3f;
            case TrashClass.SuperHeavy: return 5f;
            default: return 1f;
        }
    }

    // Optional: Get trash class for other logic
    public TrashClass TrashClass => trashClass;
}