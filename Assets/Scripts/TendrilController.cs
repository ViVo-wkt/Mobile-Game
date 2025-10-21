using Fusion;
using UnityEngine;

public class TendrilController : NetworkBehaviour
{
    [Networked]
    private Vector3 TargetPosition { get; set; }

    [Networked]
    private float MaxRange { get; set; }

    [Networked]
    private bool IsRetracting { get; set; }

    // Speed at which the tendril extends and retracts
    public float RetractSpeed = 30f;

    // Reference back to the player/launcher
    private TendrilLauncher _launcher;

    // Call from launcher on spawn
    public void Initialize(TendrilLauncher launcher, Vector3 initialTarget)
    {
        _launcher = launcher;
        TargetPosition = initialTarget;
        MaxRange = launcher.MaxTendrilRange;
        IsRetracting = false;
        // Tendril starts at the player's position
        transform.position = _launcher.transform.position;
    }

    // Called every frame by the launcher while the button is held
    public void SetTarget(Vector3 newTarget, float maxRange)
    {
        TargetPosition = newTarget;
        MaxRange = maxRange;
        IsRetracting = false;
    }

    // Called when the mouse button is released
    public void Retract()
    {
        IsRetracting = true;
    }

    public override void FixedUpdateNetwork()
    {
        if (_launcher == null)
        {
            // Spróbuj znaleŸæ referencjê do launchera, jeœli zosta³a utracona (czêste w trybie shared)
            var playerObj = Runner.GetPlayerObject(Object.InputAuthority);
            if (playerObj != null)
            {
                _launcher = playerObj.GetComponent<TendrilLauncher>();
            }
            if (_launcher == null) return;
        }

        Vector3 playerPos = _launcher.transform.position;
        Vector3 currentPos = transform.position;

        if (IsRetracting)
        {
            // --- RETRACTION LOGIC ---

            // Move the tendril back towards the player
            float step = RetractSpeed * Runner.DeltaTime;
            transform.position = Vector3.MoveTowards(currentPos, playerPos, step);

            // Check if fully retracted (close enough to player's center)
            if (Vector3.Distance(transform.position, playerPos) < 0.1f)
            {
                // Tendril has returned, destroy it
                Runner.Despawn(Object);
            }
        }
        else // Extending/Extended
        {
            // --- EXTENSION/STRETCH LOGIC ---

            // Target position of the tip
            Vector3 targetTip = TargetPosition;

            // Direction from player to target tip
            Vector3 direction = (targetTip - playerPos).normalized;

            // The object's actual position should represent the tip that extends/retracts.
            float targetDistance = Vector3.Distance(playerPos, targetTip);
            float maxExtensionStep = RetractSpeed * Runner.DeltaTime;

            // Move the object's position towards the target tip
            transform.position = Vector3.MoveTowards(currentPos, targetTip, maxExtensionStep);

            // Calculate the distance from the player to the current tip position
            float currentDistance = Vector3.Distance(playerPos, transform.position);

            // Set the scale and orientation to make it look like a stretched tentacle

            // Scale Z: Length from player to tip
            transform.localScale = new Vector3(
                transform.localScale.x,
                transform.localScale.y,
                currentDistance
            );

            // Center the object halfway between the player and the tip
            // Note: This relies on the prefab's pivot being at one end or requires a child object setup.
            // Assuming the pivot is in the center of the cylinder for simplicity:
            // transform.position = playerPos + direction * (currentDistance * 0.5f);

            // Re-orient the tendril to point towards the target
            Quaternion baseRotation = Quaternion.LookRotation(direction);
            Quaternion correctionRotation = Quaternion.Euler(-90, 0, 0);
            transform.rotation = baseRotation * correctionRotation;
        }
    }

    // (Optional: Re-add your collision logic here if needed for interaction)
    /*
    private void OnTriggerEnter(Collider other)
    {
        // ... (Deal damage or interact only when fully extended or on initial hit) ...
    }
    */
}