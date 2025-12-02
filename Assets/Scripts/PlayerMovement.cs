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

        // 1. Check Authority
        if (!Object.HasInputAuthority)
        {
            // This is expected for other players' characters on your screen
            return;
        }

        Debug.Log($"[PlayerMovement] Spawning Local UI for {gameObject.name}");

        // 2. Check Canvas
        Canvas canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError($"[PlayerMovement] CRITICAL: No Canvas found in scene! Joysticks cannot be spawned.");
            return;
        }

        // 3. Check Prefabs and Spawn
        if (MoveJoystickPrefab != null)
        {
            GameObject go = Instantiate(MoveJoystickPrefab, canvas.transform);
            var a = go.AddComponent<JoystickAnchor>();
            a.preset = JoystickAnchor.AnchorPreset.BottomLeft;
            a.offset = new Vector2(0, 0);
            a.size = new Vector2(180, 180);
            a.Apply();
            _moveJoystickInstance = go.GetComponent<CustomJoystick>();
        }
        else
        {
            Debug.LogError($"[PlayerMovement] MoveJoystickPrefab is MISSING on {gameObject.name}. Please assign it in the Inspector.");
        }

        if (AimJoystickPrefab != null)
        {
            GameObject go = Instantiate(AimJoystickPrefab, canvas.transform);
            var a = go.AddComponent<JoystickAnchor>();
            a.preset = JoystickAnchor.AnchorPreset.BottomRight;
            a.offset = new Vector2(0, 0);
            a.size = new Vector2(180, 180);
            a.Apply();
            _aimJoystickInstance = go.GetComponent<CustomJoystick>();
        }
        else
        {
            Debug.LogError($"[PlayerMovement] AimJoystickPrefab is MISSING on {gameObject.name}. Please assign it in the Inspector.");
        }

        // 4. Register Input
        NetworkInputManager.RegisterInput(Runner, _moveJoystickInstance, _aimJoystickInstance);

        StartCoroutine(WaitForCameraAndAssign());
    }

    private IEnumerator WaitForCameraAndAssign()
    {
        if (Camera.main == null) yield break;

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
        Vector3 direction = Vector3.zero;

        if (GetInput(out NetworkInputData data))
        {
            direction = new Vector3(data.MoveDirection.x, 0, data.MoveDirection.y);
            direction = Vector3.ClampMagnitude(direction, 1f);
        }

        _controller.Move(direction);

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

        if (Object.HasInputAuthority)
        {
            NetworkInputManager.UnregisterInput(runner);
        }
    }
}