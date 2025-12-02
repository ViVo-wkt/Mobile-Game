using System.Collections;
using Fusion;
using UnityEngine;

public class PlayerMovement : NetworkBehaviour
{
    private NetworkCharacterController _controller;
    private Quaternion _targetRotation;

    [Header("Movement")]
    public float RotationSmoothTime = 0.1f;

    [Header("Joystick Prefabs")]
    public GameObject MoveJoystickPrefab;
    public GameObject AimJoystickPrefab;

    [Header("Camera")]
    public Camera Camera;

    private CustomJoystick _moveJoystickInstance;
    private CustomJoystick _aimJoystickInstance;

    private void Awake()
    {
        _controller = GetComponent<NetworkCharacterController>();
    }

    public override void Spawned()
    {
        base.Spawned();

        if (!Object.HasInputAuthority) return;

        Canvas canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
        if (!canvas) { Debug.LogError("Canvas missing!"); return; }

        // Spawn LEFT Joystick (Movement)
        if (MoveJoystickPrefab)
        {
            GameObject go = Instantiate(MoveJoystickPrefab, canvas.transform);
            var a = go.AddComponent<JoystickAnchor>();
            a.preset = JoystickAnchor.AnchorPreset.BottomLeft;
            a.offset = new Vector2(0, 0);
            a.size = new Vector2(180, 180);
            a.Apply();
            _moveJoystickInstance = go.GetComponent<CustomJoystick>();

            // Register with Input Manager so the input struct can find it
            NetworkInputManager.MoveJoystick = _moveJoystickInstance;
        }

        // Spawn RIGHT Joystick (Aiming)
        if (AimJoystickPrefab)
        {
            GameObject go = Instantiate(AimJoystickPrefab, canvas.transform);
            var a = go.AddComponent<JoystickAnchor>();
            a.preset = JoystickAnchor.AnchorPreset.BottomRight;
            a.offset = new Vector2(0, 0);
            a.size = new Vector2(180, 180);
            a.Apply();
            _aimJoystickInstance = go.GetComponent<CustomJoystick>();

            // Register with Input Manager so the input struct can find it
            NetworkInputManager.AimJoystick = _aimJoystickInstance;
        }

        StartCoroutine(WaitForCameraAndAssign());
    }

    private IEnumerator WaitForCameraAndAssign()
    {
        Camera cam = null;
        while (cam == null)
        {
            cam = Camera.main;
            yield return null;
        }

        var camScript = cam.GetComponent<ThirdPersonCamera>();
        if (camScript != null)
        {
            camScript.Target = transform;
        }
    }

    public override void FixedUpdateNetwork()
    {
        // Initialize direction to zero (idle)
        Vector3 direction = Vector3.zero;

        // 1. Try to get Input
        if (GetInput(out NetworkInputData data))
        {
            // 2. Convert Vector2 input to Vector3 direction
            direction = new Vector3(data.MoveDirection.x, 0, data.MoveDirection.y);
            
            // 3. Clamp to ensure diagonal movement isn't faster than 1.0
            direction = Vector3.ClampMagnitude(direction, 1f);
        }

        // 4. MOVE ALWAYS (Crucial Fix)
        // We call this even if direction is zero. 
        // This ensures the NetworkCharacterController applies Gravity and Braking every tick.
        _controller.Move(direction);

        // 5. Rotation only happens if we are actually moving
        if (direction.sqrMagnitude > 0.001f)
        {
            _targetRotation = Quaternion.LookRotation(direction, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, _targetRotation, 
                1f - Mathf.Exp(-RotationSmoothTime / Runner.DeltaTime));
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (_moveJoystickInstance != null) Destroy(_moveJoystickInstance.gameObject);
        if (_aimJoystickInstance != null) Destroy(_aimJoystickInstance.gameObject);
    }
}