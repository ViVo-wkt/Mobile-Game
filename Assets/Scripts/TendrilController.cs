using Fusion;
using UnityEngine;

public class TendrilController : NetworkBehaviour
{
    [Networked] private Vector3 CurrentTarget { get; set; }
    [Networked] private bool IsRetracting { get; set; }
    [Networked] private float CurrentLength { get; set; }

    [Header("Settings")]
    public float ExtendSpeed = 20f;
    public float RetractSpeed = 30f;

    [Header("Visual Correction")]
    [Tooltip("If using a Unity Cylinder/Capsule, set this to 2. If using a Cube, set to 1.")]
    public float BasePrefabLength = 2f; 

    public bool IsFullyRetracted => CurrentLength <= 0.1f && IsRetracting;

    private TendrilLauncher _ownerLauncher;
    private Rigidbody _rb;

    public void Initialize(TendrilLauncher launcher)
    {
        _ownerLauncher = launcher;
        CurrentLength = 0.1f;
        
        _rb = GetComponent<Rigidbody>();
        if (_rb) 
        {
            _rb.isKinematic = true; 
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
        }
    }

    public override void Spawned()
    {
        // 1. FIND THE OWNER
        if (_ownerLauncher == null)
        {
            var playerObj = Runner.GetPlayerObject(Object.InputAuthority);
            if (playerObj != null)
            {
                _ownerLauncher = playerObj.GetComponent<TendrilLauncher>();
            }
        }

        // 2. IGNORE COLLISIONS (Run on Server AND Client)
        if (_ownerLauncher != null)
        {
            // CHANGED: Use GetComponentsInChildren to find ALL colliders on the player body/limbs
            var playerColliders = _ownerLauncher.GetComponentsInChildren<Collider>(true); 
            var myCollider = GetComponent<Collider>();

            if (myCollider != null)
            {
                foreach (var pc in playerColliders)
                {
                    // Don't ignore myself if I accidentally found myself
                    if (pc != myCollider) Physics.IgnoreCollision(myCollider, pc);
                }
            }
        }
    }

    public void SetTarget(Vector3 target)
    {
        CurrentTarget = target;
        IsRetracting = false;
    }

    public void Retract()
    {
        IsRetracting = true;
    }

    public override void FixedUpdateNetwork()
    {
        // Fail-safe: Try to find owner again if Spawned() missed it (e.g. race condition)
        if (_ownerLauncher == null) 
        {
            var playerObj = Runner.GetPlayerObject(Object.InputAuthority);
            if (playerObj != null) _ownerLauncher = playerObj.GetComponent<TendrilLauncher>();
            
            // If still null, we can't update position
            if (_ownerLauncher == null) 
            {
                if (Object.HasStateAuthority) Runner.Despawn(Object);
                return;
            }
        }

        Transform originT = _ownerLauncher.SpawnPoint ? _ownerLauncher.SpawnPoint : _ownerLauncher.transform;
        Vector3 origin = originT.position;
        float distToTarget = Vector3.Distance(origin, CurrentTarget);

        // 1. Calculate Length
        if (IsRetracting)
        {
            CurrentLength = Mathf.MoveTowards(CurrentLength, 0f, RetractSpeed * Runner.DeltaTime);
        }
        else
        {
            CurrentLength = Mathf.MoveTowards(CurrentLength, distToTarget, ExtendSpeed * Runner.DeltaTime);
        }

        // 2. Calculate Direction
        Vector3 direction = (CurrentTarget - origin).normalized;
        if (direction == Vector3.zero) direction = originT.forward;

        // 3. Physics Positioning
        Vector3 tipPosition = origin + direction * CurrentLength;
        Vector3 centerPos = Vector3.Lerp(origin, tipPosition, 0.5f);
        
        // 4. Rotation Logic
        // Stabilized LookRotation prevents spinning
        Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up) * Quaternion.Euler(90f, 0f, 0f);

        if (_rb != null)
        {
            _rb.MovePosition(centerPos);
            _rb.MoveRotation(rotation);
        }
        else
        {
            transform.position = centerPos;
            transform.rotation = rotation;
        }
    }

    public override void Render()
    {
        Vector3 s = transform.localScale;
        float yScale = CurrentLength / BasePrefabLength;
        transform.localScale = new Vector3(s.x, yScale, s.z); 
    }
}