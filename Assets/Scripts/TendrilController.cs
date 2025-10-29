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

    [Networked]
    private float CurrentLength { get; set; }

    // Speed at which the tendril extends and retracts
    public float Speed = 30f;

    // Reference back to the player/launcher
    private TendrilLauncher _launcher;

    private Rigidbody _rigidbody;
    private Collider _collider;

    // Called from launcher on spawn (on spawning client)
    public void Initialize(TendrilLauncher launcher, Vector3 initialTarget)
    {
        _launcher = launcher;
        TargetPosition = initialTarget;
        MaxRange = launcher.MaxTendrilRange;
        IsRetracting = false;
        CurrentLength = 0.1f; // Start small to avoid issues
        transform.position = _launcher.transform.position;

        // Preserve original thickness scale, set length
        Vector3 originalScale = transform.localScale;
        transform.localScale = new Vector3(originalScale.x, CurrentLength, originalScale.z);

        // Setup physics components
        _rigidbody = GetComponent<Rigidbody>();
        if (_rigidbody != null)
        {
            _rigidbody.isKinematic = true;
        }

        _collider = GetComponent<Collider>();
        if (_collider == null)
        {
            Debug.LogError("TendrilController requires a Collider!");
        }
        // Ensure it's NOT a trigger for automatic physics interaction
        if (_collider != null)
        {
            _collider.isTrigger = false;
        }
    }

    public override void Spawned()
    {
        base.Spawned();

        // Ignore collisions with all colliders on the player object and its children on all clients
        var playerObj = Runner.GetPlayerObject(Object.InputAuthority);
        if (playerObj != null)
        {
            Collider[] playerColliders = playerObj.GetComponentsInChildren<Collider>();
            foreach (var playerCollider in playerColliders)
            {
                if (playerCollider != null && _collider != null)
                {
                    Physics.IgnoreCollision(_collider, playerCollider);
                }
            }
        }
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
            // Re-find launcher if reference lost
            var playerObj = Runner.GetPlayerObject(Object.InputAuthority);
            if (playerObj != null)
            {
                _launcher = playerObj.GetComponent<TendrilLauncher>();
            }
            if (_launcher == null) return;
        }

        Vector3 playerPos = _launcher.transform.position;
        float delta = Speed * Runner.DeltaTime;

        // Compute desired length
        float desiredLength = IsRetracting ? 0.1f : Mathf.Min(Vector3.Distance(playerPos, TargetPosition), MaxRange);
        CurrentLength = Mathf.MoveTowards(CurrentLength, desiredLength, delta);

        // Despawn if fully retracted
        if (IsRetracting && CurrentLength <= 0.1f)
        {
            if (_launcher != null)
            {
                _launcher.ClearActiveTendrilRpc();
            }
            Runner.Despawn(Object);
            return;
        }

        // Update transform: position at midpoint, rotation aligned, scale length along Y
        if (CurrentLength > 0.1f)
        {
            Vector3 direction = (TargetPosition - playerPos).normalized;
            Vector3 midpoint = playerPos + direction * (CurrentLength * 0.5f);
            Quaternion rotation = Quaternion.LookRotation(Vector3.up, direction);

            // Use Rigidbody.Move for physics-aware movement (better interpolation/prediction)
            if (_rigidbody != null)
            {
                _rigidbody.MovePosition(midpoint);
                _rigidbody.MoveRotation(rotation);
            }
            else
            {
                transform.position = midpoint;
                transform.rotation = rotation;
            }

            // Scale Y for length (preserves X/Z thickness)
            Vector3 scale = transform.localScale;
            scale.y = CurrentLength;
            transform.localScale = scale;
        }
    }
}