using Fusion;
using UnityEngine;

public class PlayerMovement : NetworkBehaviour
{
    private CharacterController _controller;
    private Vector3 _inputDirection;
    private Quaternion _targetRotation;

    [Header("Movement")]
    public float PlayerSpeed = 5f;
    public float RotationSmoothTime = 0.1f;
    public float MoveSmoothTime = 0.1f;

    [Header("Joysticks")]
    public CustomJoystick MoveJoystick;     // Left
    public CustomJoystick AimJoystick;      // Right ← NEW!

    [Header("Aiming")]
    public Camera Camera;
    public float MaxTendrilRange = 10f;     // For tendril targeting

    private Vector3 _moveVelocity;

    // ← DODAJ TO POLE
    public TendrilLauncher TendrilLauncher; 

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
    }

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            Camera = Camera.main;
            if (Camera != null)
            {
                Camera.GetComponent<ThirdPersonCamera>().Target = transform;
            }
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        // MOVEMENT (Left Joystick or Keyboard)
        if (MoveJoystick != null)
        {
            Vector2 moveInput = MoveJoystick.Direction;
            _inputDirection = new Vector3(moveInput.x, 0, moveInput.y).normalized;
        }
        else
        {
            _inputDirection = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")).normalized;
        }

        // ← ALL YOUR EXISTING MOVEMENT CODE (UNCHANGED)
        Vector3 desiredVelocity = _inputDirection * PlayerSpeed;
        _moveVelocity = Vector3.Lerp(_moveVelocity, desiredVelocity, 1f - Mathf.Exp(-MoveSmoothTime / Runner.DeltaTime));
        _controller.Move(_moveVelocity * Runner.DeltaTime);

        if (_inputDirection != Vector3.zero)
        {
            _targetRotation = Quaternion.LookRotation(_inputDirection, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, _targetRotation, 1f - Mathf.Exp(-RotationSmoothTime / Runner.DeltaTime));
        }

        // ← NEW: AIMING (Right Joystick)
        UpdateAiming();
    }

    private void UpdateAiming()
    {
        if (AimJoystick == null || Camera == null) return;

        Vector2 aimInput = AimJoystick.Direction;
        if (aimInput == Vector2.zero)
        {
            // No input: Aim forward
            aimInput = Vector2.up;
        }

        // Convert screen joystick input → world direction
        Vector3 aimDirection = new Vector3(aimInput.x, 0, aimInput.y).normalized;

        // Transform to world space (relative to player facing)
        aimDirection = transform.TransformDirection(aimDirection);

        // Raycast from camera to find target point (like your existing mouse logic)
        Vector3 targetPoint = GetAimTargetPoint(aimDirection);

        // ← UPDATE TENDRIL TARGET (Your existing TendrilLauncher will use this)
        if (TendrilLauncher != null)
        {
            TendrilLauncher.SetAimTarget(targetPoint);
        }
    }

    private Vector3 GetAimTargetPoint(Vector3 direction)
    {
        Vector3 origin = transform.position;
        Plane ground = new Plane(Vector3.up, origin);

        // Ray from player toward aim direction, max range
        if (ground.Raycast(new Ray(origin, direction), out float distance))
        {
            return origin + direction * Mathf.Min(distance, MaxTendrilRange);
        }
        return origin + direction * MaxTendrilRange;
    }

    public override void Render()
    {
        if (HasStateAuthority) return;
        // ← YOUR INTERPOLATION (UNCHANGED)
        Vector3 targetPosition = transform.position;
        Quaternion targetRotation = transform.rotation;
        transform.position = Vector3.Lerp(transform.position, targetPosition, 1f - Mathf.Exp(-MoveSmoothTime / Time.deltaTime));
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 1f - Mathf.Exp(-RotationSmoothTime / Time.deltaTime));
    }
}