using UnityEngine;

[RequireComponent(typeof(Collider))]
public class KillZone : MonoBehaviour
{
    private void Reset() => GetComponent<Collider>().isTrigger = true;

    private void OnTriggerEnter(Collider other)
    {
        var respawnable = other.GetComponentInParent<IRespawnable>();
        respawnable?.Respawn();
    }
}
