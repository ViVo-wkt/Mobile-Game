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

    // Speed at which the tendril extends and retracts (using same speed for both)
    public float Speed = 30f;

    // Force strength for manipulating objects
    public float ForceStrength = 10f;

    // Reference back to the player/launcher
    private TendrilLauncher _launcher;

    private Rigidbody _rigidbody;

    // Call from launcher on spawn
    public void Initialize(TendrilLauncher launcher, Vector3 initialTarget)
    {
        _launcher = launcher;
        TargetPosition = initialTarget;
        MaxRange = launcher.MaxTendrilRange;
        IsRetracting = false;
        CurrentLength = 0.1f; // Start with a small length to avoid zero-scale issues
        transform.position = _launcher.transform.position;
        transform.localScale = new Vector3(transform.localScale.x, CurrentLength, transform.localScale.z);

        _rigidbody = GetComponent<Rigidbody>();
        if (_rigidbody != null)
        {
            _rigidbody.isKinematic = true;
        }
    }

    // Called every frame by the launcher while the button is held
    public void SetTarget(Vector3 newTarget, float maxRange)
    {
        TargetPosition = newTarget;
        MaxRange = maxRange;
        IsRetracting = false; // Interrupt retraction if button pressed again
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
            // Try to find reference to launcher if lost (common in shared mode)
            var playerObj = Runner.GetPlayerObject(Object.InputAuthority);
            if (playerObj != null)
            {
                _launcher = playerObj.GetComponent<TendrilLauncher>();
            }
            if (_launcher == null) return;
        }

        Vector3 playerPos = _launcher.transform.position;

        float delta = Speed * Runner.DeltaTime;

        float desiredLength = IsRetracting ? 0.1f : Mathf.Min(Vector3.Distance(playerPos, TargetPosition), MaxRange);
        CurrentLength = Mathf.MoveTowards(CurrentLength, desiredLength, delta);

        if (IsRetracting && CurrentLength <= 0.1f)
        {
            // Fully retracted: clear ActiveTendril on launcher and despawn
            if (_launcher != null)
            {
                _launcher.ClearActiveTendrilRpc();
            }
            Runner.Despawn(Object);
            return;
        }

        // Update position, rotation, and scale (common for both extend and retract)
        if (CurrentLength > 0f)
        {
            Vector3 direction = (TargetPosition - playerPos).normalized;
            Vector3 midpoint = playerPos + direction * (CurrentLength / 2f);
            Quaternion rotation = Quaternion.FromToRotation(Vector3.up, direction);

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

            transform.localScale = new Vector3(transform.localScale.x, CurrentLength, transform.localScale.z);
        }
        else
        {
            transform.localScale = Vector3.zero;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Only interact on the State Authority to avoid duplicates
        if (!HasStateAuthority) return;

        // Manipulate physical items (e.g., apply a force to push or pull them)
        Rigidbody rb = collision.rigidbody;
        if (rb == null || collision.contacts.Length == 0) return;

        Vector3 contactPoint = collision.contacts[0].point;
        Vector3 forceDirection;

        if (IsRetracting)
        {
            // Pull towards player
            forceDirection = (_launcher.transform.position - contactPoint).normalized;
        }
        else
        {
            // Push away from player
            forceDirection = (TargetPosition - _launcher.transform.position).normalized;
        }

        // Poprawka: u¿yj AddForceAtPosition zamiast nieistniej¹cej AddForceAtPoint
        rb.AddForceAtPosition(forceDirection * ForceStrength, contactPoint);
    }
}