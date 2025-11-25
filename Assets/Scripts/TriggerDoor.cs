using UnityEngine;
using System.Collections;

public class TriggerActivatedDoor : MonoBehaviour
{
    [Header("Activation")]
    [Tooltip("The tag of the object (X) that can activate this trigger. E.g., 'PushableBox'")]
    public string RequiredTag = "Pushable";

    [Header("Door/Wall Setup")]
    [Tooltip("The wall (or door) that you want to move.")]
    public Transform SlidingWall;

    [Tooltip("An empty GameObject marking the position the wall moves TO when activated.")]
    public Transform OpenPositionTarget;

    [Tooltip("How fast the wall moves (units per second).")]
    public float SlideSpeed = 2.0f;

    private Vector3 _closedPosition;
    private Vector3 _targetPosition;
    private Coroutine _moveCoroutine;

    void Start()
    {
        if (SlidingWall == null)
        {
            Debug.LogError("TriggerActivatedDoor: 'SlidingWall' is not assigned!", this);
            return;
        }

        if (OpenPositionTarget == null)
        {
            Debug.LogError("TriggerActivatedDoor: 'OpenPositionTarget' is not assigned!", this);
            return;
        }

        _closedPosition = SlidingWall.position;

        _targetPosition = _closedPosition;
    }

    void Update()
    {
        if (SlidingWall == null) return;

        if (Vector3.Distance(SlidingWall.position, _targetPosition) > 0.01f)
        {
            SlidingWall.position = Vector3.MoveTowards(
                SlidingWall.position,
                _targetPosition,
                SlideSpeed * Time.deltaTime
            );
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(RequiredTag))
        {
            Debug.Log($"'{other.name}' entered trigger. Opening door.");
            _targetPosition = OpenPositionTarget.position;
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(RequiredTag))
        {
            Debug.Log($"'{other.name}' exited trigger. Closing door.");
            _targetPosition = _closedPosition;
        }
    }
}
