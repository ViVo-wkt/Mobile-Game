using Fusion;
using UnityEngine;

public class SpeedBoostAbility : NetworkBehaviour
{
    [Header("Settings")]
    public float BoostMultiplier = 2.0f; // 2x Speed
    public float Duration = 2.0f;        // Lasts 2 seconds
    public float Cooldown = 5.0f;        // Wait 5 seconds
    public Color BoostColor = Color.yellow; // Visual feedback

    [Networked] public NetworkBool IsActive { get; set; }
    [Networked] private TickTimer DurationTimer { get; set; }
    [Networked] private TickTimer CooldownTimer { get; set; }

    private NetworkCharacterController _controller;
    private Color _originalColor;
    private Renderer[] _renderers;
    private float _baseSpeed;

    private void Awake()
    {
        _controller = GetComponent<NetworkCharacterController>();
        _renderers = GetComponentsInChildren<Renderer>();
    }

    public override void Spawned()
    {
        if (_controller) _baseSpeed = _controller.maxSpeed;

        // Store original color from the first renderer found
        if (_renderers.Length > 0) _originalColor = _renderers[0].material.color;
    }

    public override void FixedUpdateNetwork()
    {
        // 1. Handle Input (Activation)
        if (GetInput(out NetworkInputData input))
        {
            if (input.AbilityTriggered && !IsActive && CooldownTimer.ExpiredOrNotRunning(Runner))
            {
                ActivateAbility();
            }
        }

        // 2. Handle Duration (Deactivation)
        if (IsActive && DurationTimer.Expired(Runner))
        {
            DeactivateAbility();
        }
    }

    private void ActivateAbility()
    {
        IsActive = true;
        DurationTimer = TickTimer.CreateFromSeconds(Runner, Duration);

        // Apply Speed
        if (_controller) _controller.maxSpeed = _baseSpeed * BoostMultiplier;

        // Apply Visuals
        SetColor(BoostColor);
    }

    private void DeactivateAbility()
    {
        IsActive = false;
        CooldownTimer = TickTimer.CreateFromSeconds(Runner, Cooldown);

        // Reset Speed
        if (_controller) _controller.maxSpeed = _baseSpeed;

        // Reset Visuals
        SetColor(_originalColor);
    }

    private void SetColor(Color c)
    {
        foreach (var r in _renderers) r.material.color = c;
    }

    // Public getter for Health.cs to check
    public bool IsImmune => IsActive;
}