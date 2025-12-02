using Fusion;
using System.Collections;
using System.Linq;
using UnityEngine;

public class Turret : NetworkBehaviour
{
    [Header("Turret Settings")]
    public float Damage = 25f;
    public float FireRate = 2f;
    public float Range = 15f;

    [Header("Layers")]
    [Tooltip("Layer for the Player")]
    public LayerMask TargetLayer;
    [Tooltip("Layers that block the bullet (e.g. Default, Ground, Walls)")]
    public LayerMask ObstacleLayer;

    [Header("Visuals")]
    public Transform TurretHead;
    public Transform FirePoint;

    [Tooltip("Assign the small sphere GameObject here. It will be enabled/disabled when firing.")]
    public GameObject MuzzleFlashSphere;

    public float RotationSpeed = 5f;
    public Vector3 RotationCorrection = Vector3.zero;

    [Header("Debugging")]
    public bool ShowDebugLines = true;

    [Networked] private TickTimer AttackTimer { get; set; }

    public override void Spawned()
    {
        // Ensure the sphere is hidden at start
        if (MuzzleFlashSphere != null) MuzzleFlashSphere.SetActive(false);
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;

        NetworkObject target = FindClosestPlayer();

        if (target != null)
        {
            RotateTowards(target.transform.position);

            if (CheckLineOfSight(target))
            {
                if (AttackTimer.ExpiredOrNotRunning(Runner))
                {
                    Attack(target);
                    AttackTimer = TickTimer.CreateFromSeconds(Runner, FireRate);
                }
            }
        }
    }

    private void Attack(NetworkObject target)
    {
        // 1. Deal Damage
        var healthScript = target.GetComponent<Health>();
        if (healthScript != null)
        {
            healthScript.DealDamageRpc(Damage);
        }

        // 2. Trigger Visuals (Run on all clients)
        Rpc_FireVisuals();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void Rpc_FireVisuals()
    {
        if (MuzzleFlashSphere != null)
        {
            // If already flashing, stop the old coroutine so we don't flicker weirdly
            StopAllCoroutines();
            StartCoroutine(FlashSphere());
        }
    }

    private IEnumerator FlashSphere()
    {
        MuzzleFlashSphere.SetActive(true);
        yield return new WaitForSeconds(0.1f); // Visible for 0.1 seconds
        MuzzleFlashSphere.SetActive(false);
    }

    // --- Targeting Logic (Same as before) ---

    private bool CheckLineOfSight(NetworkObject target)
    {
        if (FirePoint == null) return false;

        Vector3 origin = FirePoint.position;
        Vector3 targetCenter = target.transform.position + Vector3.up * 1.5f;
        Vector3 direction = (targetCenter - origin).normalized;
        float distance = Vector3.Distance(origin, targetCenter);

        RaycastHit[] hits = Physics.RaycastAll(origin, direction, distance, ObstacleLayer);
        System.Array.Sort(hits, (x, y) => x.distance.CompareTo(y.distance));

        foreach (var hit in hits)
        {
            if (hit.transform.IsChildOf(transform)) continue; // Ignore Turret
            if (hit.transform.root == target.transform.root) continue; // Ignore Target

            if (ShowDebugLines) Debug.DrawLine(origin, hit.point, Color.red);
            return false;
        }

        if (ShowDebugLines) Debug.DrawLine(origin, targetCenter, Color.green);
        return true;
    }

    private void RotateTowards(Vector3 targetPos)
    {
        if (TurretHead == null) return;

        Vector3 direction = targetPos - TurretHead.position;
        direction.y = 0;

        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion lookRot = Quaternion.LookRotation(direction);
            Quaternion correctedRot = lookRot * Quaternion.Euler(RotationCorrection);
            TurretHead.rotation = Quaternion.Slerp(TurretHead.rotation, correctedRot, RotationSpeed * Runner.DeltaTime);
        }
    }

    private NetworkObject FindClosestPlayer()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, Range, TargetLayer);
        NetworkObject closestTarget = null;
        float closestDist = float.MaxValue;

        foreach (var hit in hits)
        {
            var health = hit.GetComponentInParent<Health>();
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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, Range);
    }
}