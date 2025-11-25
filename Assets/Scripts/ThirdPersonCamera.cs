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

        Vector3 desiredPosition = Target.position + (Vector3.up * CameraHeight);

        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, SmoothTime);

        transform.rotation = Quaternion.Euler(90f, 0f, 0f);
    }
}