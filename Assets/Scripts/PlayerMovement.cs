using Fusion;
using UnityEngine;

public class PlayerMovement : NetworkBehaviour
{
    private CharacterController _controller;
    private Vector3 _inputDirection;
    private Quaternion _targetRotation;

    public float PlayerSpeed = 5f;
    public float RotationSmoothTime = 0.1f; // Controls rotation smoothness
    public float MoveSmoothTime = 0.1f; // Controls movement smoothing
    public Camera Camera;
    public float JoystickRadius = 100f; // Max distance for virtual joystick (pixels)

    private Vector3 _moveVelocity; // For smoothing movement
    private Vector2 _touchStartPos;
    private bool _isTouching;

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
            else
            {
                Debug.LogError("Main camera not found. Please ensure a camera with 'MainCamera' tag exists.");
            }
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (HasStateAuthority == false)
        {
            return;
        }

        // Handle input (prioritize touch, fallback to keyboard)
        _inputDirection = Vector3.zero;
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                _touchStartPos = touch.position;
                _isTouching = true;
            }
            else if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
            {
                Vector2 touchDelta = touch.position - _touchStartPos;
                float distance = touchDelta.magnitude;

                // Clamp joystick movement to JoystickRadius
                if (distance > JoystickRadius)
                {
                    touchDelta = touchDelta.normalized * JoystickRadius;
                }

                // Convert screen-space delta to world-space direction
                Vector2 normalizedDelta = touchDelta / JoystickRadius;
                _inputDirection = new Vector3(normalizedDelta.x, 0, normalizedDelta.y).normalized;
            }
            else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                _isTouching = false;
                _inputDirection = Vector3.zero;
            }
        }
        else
        {
            // Fallback to keyboard input
            _inputDirection = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")).normalized;
            _isTouching = false;
        }

        // Calculate movement
        Vector3 desiredVelocity = _inputDirection * PlayerSpeed;
        _moveVelocity = Vector3.Lerp(_moveVelocity, desiredVelocity, 1f - Mathf.Exp(-MoveSmoothTime / Runner.DeltaTime));

        // Move the character
        _controller.Move(_moveVelocity * Runner.DeltaTime);

        // Smoothly rotate to face movement direction
        if (_inputDirection != Vector3.zero)
        {
            _targetRotation = Quaternion.LookRotation(_inputDirection, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, _targetRotation, 1f - Mathf.Exp(-RotationSmoothTime / Runner.DeltaTime));
        }
    }

    public override void Render()
    {
        if (HasStateAuthority)
        {
            return; // Local player uses FixedUpdateNetwork
        }

        // Interpolate position and rotation for remote players using the networked transform
        Vector3 targetPosition = transform.position; // NetworkObject's transform is updated by Fusion
        Quaternion targetRotation = transform.rotation;

        // Smoothly interpolate toward the networked state
        transform.position = Vector3.Lerp(transform.position, targetPosition, 1f - Mathf.Exp(-MoveSmoothTime / Time.deltaTime));
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 1f - Mathf.Exp(-RotationSmoothTime / Time.deltaTime));
    }
}