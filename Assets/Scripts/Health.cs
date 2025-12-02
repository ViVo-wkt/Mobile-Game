using Fusion;
using UnityEngine;
using TMPro;

public class Health : NetworkBehaviour
{
    [Networked, OnChangedRender(nameof(HealthChanged))]
    public float NetworkedHealth { get; set; } = 100f;

    [Header("World Space UI (Floating)")]
    [Tooltip("Will be auto-assigned from the instantiated prefab")]
    public TextMeshProUGUI HealthText;

    [Tooltip("Drag the floating HealthDisplay prefab here")]
    public GameObject HealthDisplayPrefab;

    [Header("Local HUD (Screen Corner)")]
    [Tooltip("Drag the prefab containing the HealthHUD script and 4 heart images here")]
    public GameObject LocalHudPrefab;

    private GameObject _worldHealthUIInstance;
    private HealthHUD _localHudInstance;

    void HealthChanged()
    {
        UpdateHealthUI();
    }

    public override void Spawned()
    {
        base.Spawned();

        // 1. Spawn the Local HUD (Only for the local player)
        if (Object.HasInputAuthority && LocalHudPrefab != null)
        {
            SpawnLocalHUD();
        }

        // 2. Spawn the World Space Floating Bar (Optional: typically for other players to see)
        // If you only want others to see the floaty text, check !Object.HasInputAuthority
        SpawnWorldHealthUI();

        // 3. Force an update so UI matches current health immediately
        UpdateHealthUI();
    }

    private void SpawnLocalHUD()
    {
        Canvas canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("Canvas missing! Cannot spawn Health HUD.");
            return;
        }

        GameObject hudObj = Instantiate(LocalHudPrefab, canvas.transform);
        _localHudInstance = hudObj.GetComponent<HealthHUD>();
        
        // Optional: Position it if the prefab isn't already anchored correctly
        // But usually, you set the anchors in the Prefab itself (e.g., Top Left Corner).
    }

    private void SpawnWorldHealthUI()
    {
        if (HealthDisplayPrefab == null || _worldHealthUIInstance != null) return;

        Vector3 spawnPos = transform.position + Vector3.up * 2.5f;
        _worldHealthUIInstance = Instantiate(HealthDisplayPrefab, spawnPos, Quaternion.identity);
        _worldHealthUIInstance.transform.SetParent(transform, false);

        HealthText = _worldHealthUIInstance.GetComponentInChildren<TextMeshProUGUI>();
        if (HealthText == null)
        {
            Debug.LogError("HealthDisplayPrefab must contain a TextMeshProUGUI component!");
        }
    }

    private void UpdateHealthUI()
    {
        // Update World Text (Floating)
        if (HealthText != null)
        {
            HealthText.text = $"{NetworkedHealth:F0} HP";
        }

        // Update Local HUD (Hearts)
        if (_localHudInstance != null)
        {
            _localHudInstance.UpdateDisplay(NetworkedHealth);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void DealDamageRpc(float damage)
    {
        NetworkedHealth = Mathf.Max(0, NetworkedHealth - damage);
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        // Cleanup World UI
        if (_worldHealthUIInstance != null)
        {
            Destroy(_worldHealthUIInstance);
            _worldHealthUIInstance = null;
            HealthText = null;
        }

        // Cleanup Local HUD
        if (_localHudInstance != null)
        {
            Destroy(_localHudInstance.gameObject);
            _localHudInstance = null;
        }
    }
}