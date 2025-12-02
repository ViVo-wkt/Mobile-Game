using Fusion;
using UnityEngine;

public class Barrel : NetworkBehaviour
{
    [Header("Trigger Settings")]
    [Tooltip("If the barrel tilts more than this angle (degrees), it explodes.")]
    public float KnockOverAngle = 60f; 
    
    [Tooltip("Time in seconds to wait after tipping over before exploding.")]
    public float ExplosionDelay = 0.5f;

    [Header("Damage Settings")]
    public float DamageAmount = 25f;
    public float ExplosionRadius = 4f;

    [Header("Visuals")]
    [Tooltip("Optional: Drag an explosion particle prefab here.")]
    public GameObject ExplosionVFX;

    // Track state so it only explodes once
    private bool _hasExploded = false;
    private float _tippingTimer = 0f;

    public override void FixedUpdateNetwork()
    {
        // 1. Only the State Authority (Host) manages the logic
        if (!Object.HasStateAuthority || _hasExploded) return;

        // 2. Check the angle between the Barrel's UP vector and the World's UP vector
        float angle = Vector3.Angle(transform.up, Vector3.up);

        if (angle > KnockOverAngle)
        {
            _tippingTimer += Runner.DeltaTime;

            if (_tippingTimer >= ExplosionDelay)
            {
                Explode();
            }
        }
        else
        {
            // Reset timer if it momentarily wobbles back upright
            _tippingTimer = 0f;
        }
    }

    private void Explode()
    {
        _hasExploded = true;

        // 3. Spawn Visuals (Synced via RPC)
        ExplodeVisualsRpc();

        // 4. Find all colliders in range
        Collider[] hits = Physics.OverlapSphere(transform.position, ExplosionRadius);
        
        foreach (var hit in hits)
        {
            // Check for the Health component we created earlier
            Health targetHealth = hit.GetComponent<Health>();
            
            // Apply damage if found
            if (targetHealth != null)
            {
                targetHealth.DealDamageRpc(DamageAmount);
            }
            
            // Optional: Knock back other rigidbodies
            Rigidbody rb = hit.attachedRigidbody;
            if (rb != null)
            {
                rb.AddExplosionForce(500f, transform.position, ExplosionRadius);
            }
        }
        
        // 5. Despawn the barrel itself after the explosion
        Runner.Despawn(Object);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void ExplodeVisualsRpc()
    {
        if (ExplosionVFX != null)
        {
            Instantiate(ExplosionVFX, transform.position, Quaternion.identity);
        }
    }

    // Draw the radius in the editor for easy tuning
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, ExplosionRadius);
    }
}