using Fusion;
using UnityEngine;
using TMPro;
using System.Collections;

public class Health : NetworkBehaviour
{
    [Networked, OnChangedRender(nameof(HealthChanged))]
    public float NetworkedHealth { get; set; } = 100f;

    [Networked, OnChangedRender(nameof(DeathStateChanged))]
    public NetworkBool IsDead { get; set; }

    [Header("Health UI")]
    [Tooltip("Will be auto-assigned from the instantiated prefab")]
    public TextMeshProUGUI HealthText;

    [Header("UI Prefab")]
    [Tooltip("Drag the HealthDisplay prefab here")]
    public GameObject HealthDisplayPrefab;

    [Header("Local HUD")]
    [Tooltip("Drag the prefab containing the HealthHUD script here")]
    public GameObject LocalHudPrefab;

    private GameObject _worldHealthUIInstance;
    private HealthHUD _localHudInstance;

    // --- Callbacks ---

    void HealthChanged()
    {
        UpdateHealthUI();
    }

    void DeathStateChanged()
    {
        // 1. Disable/Enable Movement
        var movement = GetComponent<PlayerMovement>();
        if (movement != null) movement.enabled = !IsDead;

        // 2. Disable/Enable Physics (Collider)
        // We use the standard CharacterController referenced by your scripts
        var charController = GetComponent<CharacterController>();
        if (charController != null)
        {
            charController.enabled = !IsDead;
        }

        // 3. Disable/Enable Visuals (Mesh)
        foreach (var renderer in GetComponentsInChildren<Renderer>())
        {
            renderer.enabled = !IsDead;
        }

        // 4. Update UI visibility
        if (_worldHealthUIInstance != null) _worldHealthUIInstance.SetActive(!IsDead);
    }

    // --- Lifecycle ---

    public override void Spawned()
    {
        base.Spawned();

        if (Object.HasInputAuthority)
        {
            SpawnLocalHUD();
        }

        SpawnWorldHealthUI();
        UpdateHealthUI();
        
        // Ensure visual state matches IsDead when joining late
        DeathStateChanged();
    }

    private void SpawnLocalHUD()
    {
        if (LocalHudPrefab == null) return;
        
        // Use UnityEngine.Object to avoid conflict with Fusion.NetworkBehaviour.Object
        Canvas canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
        
        if (canvas != null)
        {
            GameObject hudObj = Instantiate(LocalHudPrefab, canvas.transform);
            _localHudInstance = hudObj.GetComponent<HealthHUD>();
            _localHudInstance.UpdateDisplay(NetworkedHealth);
        }
    }

    private void SpawnWorldHealthUI()
    {
        if (HealthDisplayPrefab == null || _worldHealthUIInstance != null) return;

        Vector3 spawnPos = transform.position + Vector3.up * 2.5f;
        _worldHealthUIInstance = Instantiate(HealthDisplayPrefab, spawnPos, Quaternion.identity);
        _worldHealthUIInstance.transform.SetParent(transform, false);

        HealthText = _worldHealthUIInstance.GetComponentInChildren<TextMeshProUGUI>();
    }

    private void UpdateHealthUI()
    {
        if (HealthText != null) HealthText.text = $"{NetworkedHealth:F0} HP";
        if (_localHudInstance != null) _localHudInstance.UpdateDisplay(NetworkedHealth);
    }

    // --- Damage & Death Logic ---

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void DealDamageRpc(float damage)
    {
        if (IsDead) return;

        NetworkedHealth = Mathf.Max(0, NetworkedHealth - damage);

        if (NetworkedHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        IsDead = true;
        
        // FIX: Manually call the callback on the Host/State Authority
        // because OnChanged callbacks don't trigger locally by default.
        DeathStateChanged();

        StartCoroutine(RespawnCoroutine());
    }

    private IEnumerator RespawnCoroutine()
    {
        yield return new WaitForSeconds(3f);
        Respawn();
    }

    private void Respawn()
    {
        NetworkedHealth = 100f;
        IsDead = false;

        // FIX: Manually call callback again to revive locally
        DeathStateChanged();

        // Reset Position
        var netChar = GetComponent<NetworkCharacterController>();
        if (netChar != null)
        {
            netChar.Teleport(new Vector3(0, 2f, 0)); 
        }
        else
        {
            transform.position = new Vector3(0, 2f, 0);
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (_worldHealthUIInstance != null) Destroy(_worldHealthUIInstance);
        if (_localHudInstance != null) Destroy(_localHudInstance.gameObject);
    }
}