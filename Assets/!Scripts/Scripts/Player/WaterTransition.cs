using UnityEngine;

public class WaterTransition : MonoBehaviour
{
    [SerializeField] private GameObject waterPrefab;
    [SerializeField] private GameObject cameraPrefab;
    void Update()
    {
        if (cameraPrefab.transform.position.y < 1.95f)
        {
            waterPrefab.SetActive(true);
        }
        else
        {
            waterPrefab.SetActive(false);
        }
    }
}
