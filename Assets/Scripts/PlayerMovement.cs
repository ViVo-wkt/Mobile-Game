using System.Collections;
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

    [Header("Joystick Prefabs")]
    public GameObject MoveJoystickPrefab;
    public GameObject AimJoystickPrefab;

    [Header("Camera")]
    public Camera Camera;

    private CustomJoystick _moveJoystickInstance;
    private CustomJoystick _aimJoystickInstance;
    private Vector3 _moveVelocity;
    private Vector2 _lastAimInput;

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
    }

    public override void Spawned()
    {
        base.Spawned();

        if (!Object.HasInputAuthority) return;

        Canvas canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
        if (!canvas) { Debug.LogError("Canvas missing!"); return; }

        // LEFT
        if (MoveJoystickPrefab)
        {
            GameObject go = Instantiate(MoveJoystickPrefab, canvas.transform);
            var a = go.AddComponent<JoystickAnchor>();
            a.preset = JoystickAnchor.AnchorPreset.BottomLeft;
            a.offset = new Vector2(0, 0);
            a.size = new Vector2(180, 180);
            a.Apply();
            _moveJoystickInstance = go.GetComponent<CustomJoystick>();
        }

        // RIGHT
        if (AimJoystickPrefab)
        {
            GameObject go = Instantiate(AimJoystickPrefab, canvas.transform);
            var a = go.AddComponent<JoystickAnchor>();
            a.preset = JoystickAnchor.AnchorPreset.BottomRight;
            a.offset = new Vector2(0, 0);
            a.size = new Vector2(180, 180);
            a.Apply();
            _aimJoystickInstance = go.GetComponent<CustomJoystick>();
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
        else
        {
            Debug.LogError("ThirdPersonCamera missing!");
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        Vector2 moveInput = _moveJoystickInstance != null ? _moveJoystickInstance.Direction :
            new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        _inputDirection = new Vector3(moveInput.x, 0, moveInput.y).normalized;

        Vector3 desiredVelocity = _inputDirection * PlayerSpeed;
        _moveVelocity = Vector3.Lerp(_moveVelocity, desiredVelocity, 1f - Mathf.Exp(-MoveSmoothTime / Runner.DeltaTime));
        _controller.Move(_moveVelocity * Runner.DeltaTime);

        if (_inputDirection != Vector3.zero)
        {
            _targetRotation = Quaternion.LookRotation(_inputDirection, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, _targetRotation, 1f - Mathf.Exp(-RotationSmoothTime / Runner.DeltaTime));
        }

        UpdateTendrilAim();
    }

    private void UpdateTendrilAim()
    {
        if (_aimJoystickInstance == null) return;

        Vector2 aimInput = _aimJoystickInstance.Direction;
        TendrilLauncher launcher = GetComponent<TendrilLauncher>();
        if (launcher == null) return;

        if ((aimInput - _lastAimInput).sqrMagnitude < 0.01f) return;
        _lastAimInput = aimInput;

        if (aimInput.magnitude > 0.1f)
        {
            Vector2 correctedInput = new Vector2(aimInput.x, aimInput.y);
            Vector3 worldDir = new Vector3(correctedInput.x, 0, correctedInput.y).normalized;

            float extendDist = aimInput.magnitude * launcher.MaxTendrilRange;
            Vector3 target = transform.position + worldDir * extendDist;

            if (Physics.Raycast(transform.position + Vector3.up * 0.5f, worldDir, out RaycastHit hit, extendDist))
                target = hit.point;

            launcher.SetAimTarget(target);
        }
        else
        {
            launcher.SetAimTarget(Vector3.zero);
            _lastAimInput = Vector2.zero;
        }
    }

    public override void Render()
    {
        if (HasStateAuthority) return;
        Vector3 targetPosition = transform.position;
        Quaternion targetRotation = transform.rotation;
        transform.position = Vector3.Lerp(transform.position, targetPosition, 1f - Mathf.Exp(-MoveSmoothTime / Time.deltaTime));
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 1f - Mathf.Exp(-RotationSmoothTime / Time.deltaTime));
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (_moveJoystickInstance != null) Destroy(_moveJoystickInstance.gameObject);
        if (_aimJoystickInstance != null) Destroy(_aimJoystickInstance.gameObject);
    }
}