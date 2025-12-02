using System.Collections;
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement; // Added for Scene access

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

        // FIX: Find the canvas specifically in the scene where this Player exists
        Canvas canvas = FindLocalCanvas();

        if (!canvas)
        {
            Debug.LogError($"Canvas missing in scene {gameObject.scene.name}!");
            return;
        }

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
        }

        // FIX: Register with the Manager using the specific Runner instance
        NetworkInputManager.RegisterInput(Runner, _moveJoystickInstance, _aimJoystickInstance);

        StartCoroutine(WaitForCameraAndAssign());
    }

    // Helper to find a Canvas in the same scene as this object (supports Multi-Peer)
    private Canvas FindLocalCanvas()
    {
        Scene myScene = gameObject.scene;

        // Iterate through root objects in this specific scene
        if (myScene.IsValid())
        {
            foreach (GameObject rootObj in myScene.GetRootGameObjects())
            {
                Canvas c = rootObj.GetComponentInChildren<Canvas>(true);
                if (c != null) return c;
            }
        }

        // Fallback for single player/standard builds
        return UnityEngine.Object.FindFirstObjectByType<Canvas>();
    }

    private IEnumerator WaitForCameraAndAssign()
    {
        // Safety check loop for camera
        while (Camera.main == null)
        {
            yield return null;
        }

        // In Multi-Peer, Camera.main might point to the wrong camera (Host's camera),
        // but typically specialized camera logic is needed for split-screen/multi-peer.
        // For now, we assume Camera.main works for the active view.
        Camera = Camera.main;

        var camScript = Camera.GetComponent<ThirdPersonCamera>();
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

        // Unregister to keep dictionary clean
        if (Object.HasInputAuthority)
        {
            NetworkInputManager.UnregisterInput(runner);
        }
    }
}