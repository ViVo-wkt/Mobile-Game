using Fusion;
using UnityEngine;

public class TendrilLauncher : NetworkBehaviour
{
    public NetworkPrefabRef TendrilPrefab;
    public PlayerMovement PlayerMovement;
    public float MaxTendrilRange = 10f;

    // Track the active tendril object (must be a NetworkObject)
    [Networked]
    private NetworkObject ActiveTendril { get; set; }

    private bool isAttacking;

    void Update()
    {
        // Only the Input Authority client handles local input
        if (!HasInputAuthority)
        {
            return;
        }

        bool wasAttacking = isAttacking;
        isAttacking = Input.GetKey(KeyCode.Mouse0); // LMB

        if (isAttacking)
        {
            // Calculate the target world position for the tendril tip
            Vector3 targetPoint = CalculateTargetPoint();

            if (!wasAttacking && ActiveTendril == null)
            {
                // Attack pressed for the first time: Spawn the tendril
                SpawnTendril(targetPoint);
            }

            // If the tendril exists, update its target in FixedUpdateNetwork via state
            if (ActiveTendril != null)
            {
                ActiveTendril.GetComponent<TendrilController>().SetTarget(targetPoint, MaxTendrilRange);
            }
        }
        else if (wasAttacking && ActiveTendril != null)
        {
            // Mouse button released: Trigger retraction
            ActiveTendril.GetComponent<TendrilController>().Retract();
        }
    }

    private Vector3 CalculateTargetPoint()
    {
        if (PlayerMovement == null || PlayerMovement.Camera == null)
        {
            return Vector3.zero;
        }

        Ray ray = PlayerMovement.Camera.ScreenPointToRay(Input.mousePosition);
        Plane gamePlane = new Plane(Vector3.up, Vector3.zero);

        if (gamePlane.Raycast(ray, out float distance))
        {
            Vector3 hitPoint = ray.GetPoint(distance);
            Vector3 playerPosition = transform.position;

            Vector3 direction = (hitPoint - playerPosition);
            direction.y = 0f; // Keep it on the XZ plane

            // Limit the distance to MaxTendrilRange
            if (direction.magnitude > MaxTendrilRange)
            {
                direction = direction.normalized * MaxTendrilRange;
            }

            // The target point is the player position plus the (clamped) direction
            return playerPosition + direction;
        }

        // If raycast fails, use the maximum range in the current forward direction
        return transform.position + transform.forward * MaxTendrilRange;
    }

    private void SpawnTendril(Vector3 initialTarget)
    {
        // Check if prefab is valid
        if (!TendrilPrefab.IsValid) return;

        // Spawn at player position (center of capsule)
        Vector3 spawnPosition = transform.position;

        // Initial direction and rotation (align Y-axis to direction)
        Vector3 initialDirection = (initialTarget - spawnPosition).normalized;
        Quaternion initialRotation = Quaternion.FromToRotation(Vector3.up, initialDirection);

        NetworkObject newTendril = Runner.Spawn(
            TendrilPrefab,
            spawnPosition,
            initialRotation,
            inputAuthority: Object.InputAuthority,
            (runner, obj) =>
            {
                // Initialize the controller
                TendrilController controller = obj.GetComponent<TendrilController>();
                if (controller != null)
                {
                    controller.Initialize(this, initialTarget);
                }
            }
        );

        // Assign the Networked property to track it across the network
        ActiveTendril = newTendril;
    }

    // Method to clear ActiveTendril (called via RPC from TendrilController on despawn)
    [Rpc(RpcSources.All, RpcTargets.All)]
    public void ClearActiveTendrilRpc()
    {
        ActiveTendril = null;
    }
}