using Fusion;
using UnityEngine;

public class RaycastAttack : NetworkBehaviour
{
    public float Damage = 10f;
    public PlayerMovement PlayerMovement;
    public float RaycastDistance = 100f;
    public float LineWidth = 0.1f;
    public Material LineMaterial; // Assign a material in the Inspector

    private LineRenderer lineRenderer;
    private bool isAttacking;

    void Start()
    {
        // Initialize LineRenderer
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        
        // Create fallback material if none assigned
        if (LineMaterial == null)
        {
            LineMaterial = new Material(Shader.Find("Unlit/Color"));
            LineMaterial.color = Color.red; // Bright red for visibility
        }
        
        lineRenderer.material = LineMaterial;
        lineRenderer.startWidth = LineWidth;
        lineRenderer.endWidth = LineWidth;
        lineRenderer.positionCount = 2;
        lineRenderer.useWorldSpace = true; // Ensure world space for correct positioning
        lineRenderer.enabled = false; // Start with line disabled
        
        // Set default texture mode and alignment for visibility
        lineRenderer.textureMode = LineTextureMode.Stretch;
        lineRenderer.alignment = LineAlignment.View;

        // Ensure LineRenderer is on a visible layer
        gameObject.layer = LayerMask.NameToLayer("Default"); // Adjust if needed

    }

    void Update()
    {
        if (HasStateAuthority == false)
        {
            return;
        }

        // Check for mouse input
        bool wasAttacking = isAttacking;
        isAttacking = Input.GetKey(KeyCode.Mouse1);

        if (isAttacking)
        {
            // Get the mouse position in screen space
            Vector3 mousePosition = Input.mousePosition;

            // Convert mouse position to a ray from the camera
            Ray ray = PlayerMovement.Camera.ScreenPointToRay(mousePosition);

            // Assume the game plane is at Y=0
            Plane gamePlane = new Plane(Vector3.up, Vector3.zero);

            // Calculate where the ray intersects the game plane
            if (gamePlane.Raycast(ray, out float distance))
            {
                // Get the point on the plane where the mouse is pointing
                Vector3 hitPoint = ray.GetPoint(distance);

                // Calculate the direction from the player to the mouse point on the XZ plane
                Vector3 playerPosition = transform.position;
                Vector3 direction = (hitPoint - playerPosition).normalized;
                direction.y = 0f; // Keep it on the 2D plane

                // Calculate the end point of the line (limited by RaycastDistance)
                Vector3 endPoint = playerPosition + direction * RaycastDistance;

                // Update LineRenderer positions
                lineRenderer.enabled = true;
                lineRenderer.SetPosition(0, playerPosition);
                lineRenderer.SetPosition(1, endPoint);

                // Perform the raycast only on initial click
                if (!wasAttacking) // Only trigger damage on initial press
                {
                    ray = new Ray(playerPosition, direction);
                    if (Physics.Raycast(ray, out RaycastHit hitInfo, RaycastDistance))
                    {
                        Health health = hitInfo.collider.GetComponent<Health>();
                        if (health != null)
                        {
                            // Hit registered; applying damage.
                            health.DealDamageRpc(Damage);
                        }
                    }
                }
            }
        }
        else
        {
            // Retract the line when mouse button is released
            lineRenderer.enabled = false;
        }
    }
}