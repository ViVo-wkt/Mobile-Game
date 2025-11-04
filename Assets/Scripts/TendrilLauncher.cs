using Fusion;
using UnityEngine;

public class TendrilLauncher : NetworkBehaviour
{
    public NetworkPrefabRef TendrilPrefab;
    public PlayerMovement PlayerMovement; // ← Assign in prefab (same object)
    public float MaxTendrilRange = 10f;

    // Track the active tendril object (must be a NetworkObject)
    [Networked]
    private NetworkObject ActiveTendril { get; set; }

    // Store current aim target (set by PlayerMovement via right joystick)
    [Networked] private Vector3 AimTargetPoint { get; set; }

    private bool isAttacking;

    void Update()
    {
        // Only the Input Authority client handles local input
        if (!HasInputAuthority) return;

        bool wasAttacking = isAttacking;
        isAttacking = Input.GetKey(KeyCode.Mouse0); // LMB (or touch fire button)

        if (isAttacking)
        {
            Vector3 targetPoint = AimTargetPoint; // ← Uses joystick aim!

            if (!wasAttacking && ActiveTendril == null)
            {
                SpawnTendril(targetPoint);
            }

            if (ActiveTendril != null)
            {
                ActiveTendril.GetComponent<TendrilController>().SetTarget(targetPoint, MaxTendrilRange);
            }
        }
        else if (wasAttacking && ActiveTendril != null)
        {
            ActiveTendril.GetComponent<TendrilController>().Retract();
        }
    }

    private void SpawnTendril(Vector3 initialTarget)
    {
        if (!TendrilPrefab.IsValid) return;

        Vector3 spawnPosition = transform.position;
        Vector3 initialDirection = (initialTarget - spawnPosition).normalized;
        Quaternion initialRotation = Quaternion.FromToRotation(Vector3.up, initialDirection);

        NetworkObject newTendril = Runner.Spawn(
            TendrilPrefab,
            spawnPosition,
            initialRotation,
            inputAuthority: Object.InputAuthority,
            (runner, obj) =>
            {
                TendrilController controller = obj.GetComponent<TendrilController>();
                if (controller != null)
                {
                    controller.Initialize(this, initialTarget);
                }
            }
        );

        ActiveTendril = newTendril;
    }

    // Called by PlayerMovement when right joystick moves
    public void SetAimTarget(Vector3 targetPoint)
    {
        AimTargetPoint = targetPoint;
    }

    // Called by TendrilController on despawn
    [Rpc(RpcSources.All, RpcTargets.All)]
    public void ClearActiveTendrilRpc()
    {
        ActiveTendril = null;
    }

    // Optional: Fallback to mouse if no joystick
    private Vector3 CalculateMouseFallback()
    {
        if (PlayerMovement == null || PlayerMovement.Camera == null) return transform.forward * MaxTendrilRange;

        Ray ray = PlayerMovement.Camera.ScreenPointToRay(Input.mousePosition);
        Plane ground = new Plane(Vector3.up, transform.position);

        if (ground.Raycast(ray, out float distance))
        {
            Vector3 hit = ray.GetPoint(distance);
            Vector3 dir = (hit - transform.position);
            dir.y = 0;
            return transform.position + Vector3.ClampMagnitude(dir, MaxTendrilRange);
        }

        return transform.position + transform.forward * MaxTendrilRange;
    }
}