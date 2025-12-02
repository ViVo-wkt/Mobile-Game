using Fusion;
using UnityEngine;

public class TendrilLauncher : NetworkBehaviour
{
    [Header("Settings")]
    public NetworkPrefabRef TendrilPrefab;
    public float MaxTendrilRange = 10f;
    public Transform SpawnPoint; // Assign a bone (e.g., Spine) or leave empty to use transform

    [Networked] private NetworkObject ActiveTendril { get; set; }
    
    // Sync the target point so all clients know where it's going
    [Networked] public Vector3 NetworkedAimTarget { get; set; }

    public override void FixedUpdateNetwork()
    {
        // 1. Get Input from the struct
        if (GetInput(out NetworkInputData input))
        {
            Vector3 aimDir = new Vector3(input.AimDirection.x, 0, input.AimDirection.y);
            
            // 2. Logic: If joystick is pushed, extend; otherwise retract.
            if (aimDir.sqrMagnitude > 0.01f)
            {
                // Calculate target position based on joystick magnitude (analog control)
                float extendDistance = aimDir.magnitude * MaxTendrilRange;
                Vector3 origin = SpawnPoint ? SpawnPoint.position : transform.position;
                Vector3 targetPos = origin + aimDir.normalized * extendDistance;
                
                // Optional: Raycast to stop at walls
                if (Physics.Raycast(origin + Vector3.up * 0.5f, aimDir.normalized, out RaycastHit hit, extendDistance, LayerMask.GetMask("Default")))
                {
                   targetPos = hit.point;
                }

                HandleTendrilSpawnOrUpdate(targetPos);
            }
            else
            {
                HandleRetraction();
            }
        }
        else
        {
             HandleRetraction();
        }
    }

    private void HandleTendrilSpawnOrUpdate(Vector3 targetPos)
    {
        // Only the State Authority (Server) spawns/updates NetworkObjects
        if (!Object.HasStateAuthority) return;

        NetworkedAimTarget = targetPos;

        if (ActiveTendril == null)
        {
            // Spawn new tendril
            ActiveTendril = Runner.Spawn(
                TendrilPrefab, 
                SpawnPoint ? SpawnPoint.position : transform.position, 
                Quaternion.identity, 
                Object.InputAuthority
            );
            
            // Initialize
            ActiveTendril.GetComponent<TendrilController>().Initialize(this);
        }

        // Update existing tendril
        if (ActiveTendril != null)
        {
            ActiveTendril.GetComponent<TendrilController>().SetTarget(targetPos);
        }
    }

    private void HandleRetraction()
    {
        if (!Object.HasStateAuthority) return;
        
        NetworkedAimTarget = Vector3.zero;

        if (ActiveTendril != null)
        {
            var controller = ActiveTendril.GetComponent<TendrilController>();
            if (controller != null)
            {
                controller.Retract();
                
                // If fully retracted, despawn it
                if (controller.IsFullyRetracted)
                {
                    Runner.Despawn(ActiveTendril);
                    ActiveTendril = null;
                }
            }
        }
    }
}