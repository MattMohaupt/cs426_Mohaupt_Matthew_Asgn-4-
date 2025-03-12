using UnityEngine;
using Unity.Netcode;

public class Target : NetworkBehaviour
{
    private bool _isCorrect;
    public bool isCorrect
    {
        get => _isCorrect;
        set
        {
            _isCorrect = value;
            // Disable physics for wrong targets
            if (TryGetComponent<Rigidbody>(out var rb))
            {
                rb.isKinematic = !value; // Wrong targets: kinematic (no physics)
            }
        }
    }
    // public bool isCorrect
    // {
    //     get => _isCorrect;
    //     set
    //     {
    //         _isCorrect = value;
    //         // Disable physics for wrong targets
    //         if (TryGetComponent<Rigidbody>(out var rb))
    //         {
    //             rb.isKinematic = !value; // Wrong targets: kinematic (no physics)
    //         }
    //     }
    // }

    public questionscript questionManager;
    private ulong shooterId;
        private void Start()
    {
        // 🔹 Automatically find the question manager if not assigned
        if (questionManager == null)
        {
            questionManager = FindObjectOfType<questionscript>();
            if (questionManager == null)
            {
                Debug.LogError("⚠️ questionManager is missing! Assign it in the Inspector.");
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsServer) return; // Ensure only the server processes collisions

        if (collision.gameObject.CompareTag("Bullet")) // Ensure it’s a bullet
        {
            Bullet bullet = collision.gameObject.GetComponent<Bullet>();
            if (bullet != null)
            {
                SetShooterId(bullet.shooterId.Value); // Assign shooter ID
            }

            if (isCorrect) // Correct answer logic
            {
                Debug.Log($"Correct target hit: {gameObject.name}. Destroying...");
                questionManager.OnCorrectAnswer(shooterId);
                DestroyTargetServerRpc();
            }
            else // Wrong answer logic (Stop movement)
            {
                if (TryGetComponent<Rigidbody>(out var rb))
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
            }
        }
    }

    public void SetShooterId(ulong id)
    {
        shooterId = id;
    }

    [ServerRpc(RequireOwnership = false)]
    public void DestroyTargetServerRpc()
    {
        GetComponent<NetworkObject>().Despawn(true);
        Destroy(gameObject);
    }
    [ServerRpc(RequireOwnership = false)]
    public void SetIsCorrectServerRpc(bool value)
    {
        isCorrect = value; // Properly update isCorrect across the network
    }
}