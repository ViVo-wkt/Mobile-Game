using Fusion;
using UnityEngine;
using TMPro;

public class Health : NetworkBehaviour
{
    [Networked, OnChangedRender(nameof(HealthChanged))]
    public float NetworkedHealth { get; set; } = 100f;

    [Header("Health UI")]
    [Tooltip("Will be auto-assigned from the instantiated prefab")]
    public TextMeshProUGUI HealthText;

    [Header("UI Prefab")]
    [Tooltip("Drag the HealthDisplay prefab here")]
    public GameObject HealthDisplayPrefab;

    private GameObject _healthUIInstance;

    void HealthChanged()
    {
        UpdateHealthUI();
    }

    public override void Spawned()
    {
        base.Spawned();

        // Only the local player spawns their own health UI
        if (Object.HasInputAuthority)
        {
            SpawnHealthUI();
        }

        UpdateHealthUI(); // Initial update
    }

    private void SpawnHealthUI()
    {
        if (HealthDisplayPrefab == null || _healthUIInstance != null) return;

        // Spawn UI above player
        Vector3 spawnPos = transform.position + Vector3.up * 2.5f;
        _healthUIInstance = Instantiate(HealthDisplayPrefab, spawnPos, Quaternion.identity);
        _healthUIInstance.transform.SetParent(transform, false); // World position preserved

        // Get the TMP component
        HealthText = _healthUIInstance.GetComponentInChildren<TextMeshProUGUI>();
        if (HealthText == null)
        {
            Debug.LogError("HealthDisplayPrefab must contain a TextMeshProUGUI component!");
        }
    }

    private void UpdateHealthUI()
    {
        if (HealthText != null)
        {
            HealthText.text = $"{NetworkedHealth:F0} HP";
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void DealDamageRpc(float damage)
    {
        NetworkedHealth = Mathf.Max(0, NetworkedHealth - damage);
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (_healthUIInstance != null)
        {
            Destroy(_healthUIInstance);
            _healthUIInstance = null;
            HealthText = null;
        }
    }
}