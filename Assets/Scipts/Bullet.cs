using UnityEngine;
using Unity.Netcode; // Import Netcode for multiplayer support

public class Bullet : NetworkBehaviour
{
    public NetworkVariable<ulong> shooterId = new NetworkVariable<ulong>(); // Networked shooter ID

    private void Start()
    {
        Destroy(gameObject, 5f); // Auto-destroy bullet after 5 seconds
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (IsServer) // Only the server should handle bullet destruction
        {
            Destroy(gameObject);
        }
    }

    [ServerRpc]
    public void SetShooterIdServerRpc(ulong id)
    {
        shooterId.Value = id; // Set the shooter’s ID
    }
}
