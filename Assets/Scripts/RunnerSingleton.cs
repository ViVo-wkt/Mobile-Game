using Fusion;
using UnityEngine;

public class RunnerSingleton : MonoBehaviour
{
    private void Awake()
    {
        // Check if a NetworkRunner already exists (passed from the Menu)
        NetworkRunner existingRunner = FindFirstObjectByType<NetworkRunner>();

        // If we found a runner, and it is NOT this specific component...
        if (existingRunner != null && existingRunner.gameObject != this.gameObject)
        {
            Debug.Log("Existing Runner detected. Destroying Scene Runner to prevent duplicates.");
            Destroy(this.gameObject);
        }
    }
}