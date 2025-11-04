using Fusion;
using UnityEngine;

public class TendrilController : NetworkBehaviour
{
    [Networked] private Vector3 TargetPosition { get; set; }
    [Networked] private float MaxRange { get; set; }
    [Networked] private bool IsRetracting { get; set; }
    [Networked] private float CurrentLength { get; set; }

    public float Speed = 30f;
    private TendrilLauncher _launcher;
    private Rigidbody _rigidbody;
    private Collider _collider;

    public void Initialize(TendrilLauncher launcher, Vector3 initialTarget)
    {
        _launcher = launcher;
        TargetPosition = initialTarget;
        MaxRange = launcher.MaxTendrilRange;
        IsRetracting = false;
        CurrentLength = 0.1f;
        transform.position = _launcher.transform.position;

        Vector3 originalScale = transform.localScale;
        transform.localScale = new Vector3(originalScale.x, CurrentLength, originalScale.z);

        _rigidbody = GetComponent<Rigidbody>();
        if (_rigidbody != null) _rigidbody.isKinematic = true;

        _collider = GetComponent<Collider>();
        if (_collider != null) _collider.isTrigger = false;
    }

    public void SetTarget(Vector3 newTarget, float maxRange)
    {
        TargetPosition = newTarget;
        MaxRange = maxRange;
        IsRetracting = false; // Interrupt retract
    }

    public void Retract()
    {
        IsRetracting = true;
    }

    public override void FixedUpdateNetwork()
    {
        if (_launcher == null)
        {
            var playerObj = Runner.GetPlayerObject(Object.InputAuthority);
            if (playerObj != null) _launcher = playerObj.GetComponent<TendrilLauncher>();
            if (_launcher == null) return;
        }

        Vector3 playerPos = _launcher.transform.position;
        float delta = Speed * Runner.DeltaTime;

        float desiredLength = IsRetracting ? 0.1f : Mathf.Min(Vector3.Distance(playerPos, TargetPosition), MaxRange);
        CurrentLength = Mathf.MoveTowards(CurrentLength, desiredLength, delta);

        if (IsRetracting && CurrentLength <= 0.1f)
        {
            if (_launcher != null) _launcher.ClearActiveTendrilRpc();
            Runner.Despawn(Object);
            return;
        }

        if (CurrentLength > 0.1f)
        {
            Vector3 direction = (TargetPosition - playerPos).normalized;
            Vector3 midpoint = playerPos + direction * (CurrentLength * 0.5f);
            Quaternion rotation = Quaternion.LookRotation(Vector3.up, direction);

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

            Vector3 scale = transform.localScale;
            scale.y = CurrentLength;
            transform.localScale = scale;
        }
    }

    public override void Spawned()
    {
        base.Spawned();
        var playerObj = Runner.GetPlayerObject(Object.InputAuthority);
        if (playerObj != null)
        {
            Collider[] playerColliders = playerObj.GetComponentsInChildren<Collider>();
            foreach (var pc in playerColliders)
            {
                if (_collider != null) Physics.IgnoreCollision(_collider, pc);
            }
        }
    }
}