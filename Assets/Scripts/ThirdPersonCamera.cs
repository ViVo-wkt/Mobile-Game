using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    public Transform Target;
    public float CameraHeight = 10f;
    public float SmoothTime = 0.1f;

    private Vector3 velocity = Vector3.zero;

    void LateUpdate()
    {
        if (Target == null)
        {
            return;
        }

        // Calculate desired camera position directly above the target
        Vector3 desiredPosition = Target.position + (Vector3.up * CameraHeight);

        // Smoothly move to desired position
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, SmoothTime);

        // Set rotation to look straight down (90 degrees)
        transform.rotation = Quaternion.Euler(90f, 0f, 0f);
    }
}