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
        isAttacking = Input.GetKey(KeyCode.Mouse1); // RMB

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
                // This RPC-like function sets a networked state value on the tendril.
                // In Shared Mode, the Input Authority can call methods on its controlled object.
                ActiveTendril.GetComponent<TendrilController>().SetTarget(targetPoint, MaxTendrilRange);
            }
        }
        else if (wasAttacking && ActiveTendril != null)
        {
            // Mouse button released: Trigger retraction and destruction
            ActiveTendril.GetComponent<TendrilController>().Retract();
            ActiveTendril = null; // Clear the local reference immediately
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
        // SprawdŸ, czy prefab jest poprawny
        if (!TendrilPrefab.IsValid) return;

        // Spawn slightly ahead of player
        Vector3 spawnPosition = transform.position + (initialTarget - transform.position).normalized * 0.5f;

        // Initial orientation correction (same as before)
        Quaternion baseRotation = Quaternion.LookRotation((initialTarget - transform.position).normalized);
        Quaternion correctionRotation = Quaternion.Euler(-90, 0, 0);
        Quaternion finalRotation = baseRotation * correctionRotation;

        NetworkObject newTendril = Runner.Spawn(
            TendrilPrefab,
            spawnPosition,
            finalRotation,
            inputAuthority: Object.InputAuthority,
            (runner, obj) =>
            {
                // Initialize the controller
                TendrilController controller = obj.GetComponent<TendrilController>();
                if (controller != null)
                {
                    // Pass the launcher and initial target
                    controller.Initialize(this, initialTarget);
                }
            }
        );

        // Assign the Networked property to track it across the network
        ActiveTendril = newTendril;
    }
}