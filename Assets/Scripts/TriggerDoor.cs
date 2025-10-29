using UnityEngine;
using System.Collections; // Using System.Collections for Coroutines (smooth movement)

/// <summary>
/// This script goes on the Trigger Zone object.
/// It detects when an object with a specific tag enters/exits,
/// and smoothly moves a target (like a wall or door) between two points.
/// </summary>
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

    // --- Private Variables ---
    private Vector3 _closedPosition;
    private Vector3 _targetPosition;
    private Coroutine _moveCoroutine;

    /// <summary>
    /// Store the wall's starting position as the "closed" position.
    /// </summary>
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

        // Record the wall's starting position as its "closed" state.
        _closedPosition = SlidingWall.position;
        // Start in the closed position.
        _targetPosition = _closedPosition;
    }

    /// <summary>
    /// Smoothly moves the wall towards its current _targetPosition every frame.
    /// </summary>
    void Update()
    {
        if (SlidingWall == null) return;

        // Check if the wall is already at the target position.
        if (Vector3.Distance(SlidingWall.position, _targetPosition) > 0.01f)
        {
            // Move towards the target position at a set speed.
            SlidingWall.position = Vector3.MoveTowards(
                SlidingWall.position,
                _targetPosition,
                SlideSpeed * Time.deltaTime
            );
        }
    }

    /// <summary>
    /// Called by Unity when a Collider enters this trigger.
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        // Check if the object that entered has the tag we're looking for.
        if (other.CompareTag(RequiredTag))
        {
            Debug.Log($"'{other.name}' entered trigger. Opening door.");
            // Set the target to the OPEN position.
            _targetPosition = OpenPositionTarget.position;
        }
    }

    /// <summary>
    /// Called by Unity when a Collider exits this trigger.
    /// </summary>
    private void OnTriggerExit(Collider other)
    {
        // Check if the object that left has the tag we're looking for.
        if (other.CompareTag(RequiredTag))
        {
            Debug.Log($"'{other.name}' exited trigger. Closing door.");
            // Set the target back to the CLOSED position.
            _targetPosition = _closedPosition;
        }
    }
}
