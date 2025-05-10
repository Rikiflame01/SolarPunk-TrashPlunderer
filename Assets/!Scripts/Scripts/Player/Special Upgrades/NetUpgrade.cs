using UnityEngine;

public class NetUpgrade : MonoBehaviour
{
    [SerializeField] private GameObject[] netPrefab;

    private void OnEnable()
    {
        ActionManager.OnTrashNetUpgrade += OnTrashNetUpgrade;
    }

    private void OnDisable()
    {
        ActionManager.OnTrashNetUpgrade -= OnTrashNetUpgrade;
    }

    private void OnTrashNetUpgrade()
    {
        foreach (GameObject net in netPrefab)
        {
            net.SetActive(true);
        }
    }


}
