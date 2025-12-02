using Fusion;
using UnityEngine;

public class Turret : NetworkBehaviour
{
    [Header("Turret Settings")]
    public float Damage = 25f;       // 1 Heart
    public float FireRate = 2f;      // Seconds between shots
    public float Range = 15f;        // Detection radius
    public LayerMask TargetLayer;    // Layer to search for players (e.g., Default or Player)
    public LayerMask ObstacleLayer;  // Layer for walls/obstacles blocking sight

    [Header("Visuals")]
    public Transform TurretHead;     // Assign the rotating part of the turret here
    public Transform FirePoint;      // Point where the shot originates (for raycasting)
    public float RotationSpeed = 5f;

    [Networked] private TickTimer AttackTimer { get; set; }

    // Run logic only on the server/host to prevent cheating and desync
    public override void FixedUpdateNetwork()
    {
        // Only the State Authority (Host/Server) calculates logic
        if (!Object.HasStateAuthority) return;

        NetworkObject target = FindClosestPlayer();

        if (target != null)
        {
            // 1. Rotate towards target
            RotateTowards(target.transform.position);

            // 2. Check Line of Sight
            if (CheckLineOfSight(target))
            {
                // 3. Attack if cooldown is finished
                if (AttackTimer.ExpiredOrNotRunning(Runner))
                {
                    Attack(target);
                    AttackTimer = TickTimer.CreateFromSeconds(Runner, FireRate);
                }
            }
        }
    }

    private NetworkObject FindClosestPlayer()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, Range, TargetLayer);
        NetworkObject closestTarget = null;
        float closestDist = float.MaxValue;

        foreach (var hit in hits)
        {
            // Look for the Health component on the root of the object
            var health = hit.GetComponentInParent<Health>();

            // Only target valid, living players
            if (health != null && !health.IsDead)
            {
                float dist = Vector3.Distance(transform.position, health.transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closestTarget = health.Object;
                }
            }
        }

        return closestTarget;
    }

    private bool CheckLineOfSight(NetworkObject target)
    {
        Vector3 origin = FirePoint != null ? FirePoint.position : transform.position;
        Vector3 direction = (target.transform.position - origin).normalized;
        float distance = Vector3.Distance(origin, target.transform.position);

        // Raycast to see if we hit an obstacle before the player
        if (Physics.Raycast(origin, direction, out RaycastHit hit, distance, ObstacleLayer))
        {
            // If we hit something that isn't the player, line of sight is blocked
            // (Assuming obstacles are on the ObstacleLayer and Players are not, 
            // or the Player collider is excluded from ObstacleLayer)
            if (hit.collider.gameObject != target.gameObject)
            {
                return false;
            }
        }

        return true;
    }

    private void RotateTowards(Vector3 targetPos)
    {
        if (TurretHead == null) return;

        Vector3 direction = targetPos - TurretHead.position;
        direction.y = 0; // Keep rotation horizontal only (remove if you want 3D aiming)

        if (direction != Vector3.zero)
        {
            Quaternion lookRot = Quaternion.LookRotation(direction);
            TurretHead.rotation = Quaternion.Slerp(TurretHead.rotation, lookRot, RotationSpeed * Runner.DeltaTime);
        }
    }

    private void Attack(NetworkObject target)
    {
        // Get the Health component and deal damage
        // Since we are StateAuthority, we can call the RPC or modify data.
        // Your Health.cs uses an RPC, so we call that.
        var healthScript = target.GetComponent<Health>();
        if (healthScript != null)
        {
            Debug.Log($"Turret shooting {target.name} for {Damage} damage!");
            healthScript.DealDamageRpc(Damage);
        }
    }

    // Visualize the range in the Editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, Range);
    }
}