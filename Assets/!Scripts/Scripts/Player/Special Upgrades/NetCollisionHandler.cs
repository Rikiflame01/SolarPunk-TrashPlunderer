using UnityEngine;

public class NetCollisionHandler : MonoBehaviour
{
    [SerializeField, Tooltip("Reference to the NetUpgrade script on the player")]
    private NetUpgrade netUpgrade;

    [SerializeField, Tooltip("Enable debug logs for collision handling")]
    private bool debug = true;

    private void OnValidate()
    {
        if (netUpgrade == null)
        {
            Debug.LogWarning($"NetUpgrade reference is not assigned in {name}!");
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (netUpgrade == null)
        {
            if (debug)
                Debug.LogWarning($"NetCollisionHandler on {name}: NetUpgrade reference is null. Collision ignored.");
            return;
        }

        if (collision.gameObject.layer == LayerMask.NameToLayer("Trash"))
        {
            if (debug)
                Debug.Log($"Net {name} collided with {collision.gameObject.name} on Trash layer.");
            netUpgrade.HandleNetCollision(collision);
        }
    }
}