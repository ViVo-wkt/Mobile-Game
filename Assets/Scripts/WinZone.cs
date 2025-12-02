using Fusion;
using UnityEngine;
using TMPro; // Include if you want to change text dynamically

public class WinZone : NetworkBehaviour
{
    [Header("Settings")]
    [Tooltip("Number of players required inside the zone to win.")]
    public int PlayersRequired = 2;

    [Header("UI Reference")]
    [Tooltip("Assign the Victory Panel GameObject from your Canvas here.")]
    public GameObject VictoryScreenObject;

    // Networked variable to track if the game is won.
    // The OnChangedRender attribute ensures the function 'OnVictoryStateChanged' runs on all clients when this value changes.
    [Networked, OnChangedRender(nameof(OnVictoryStateChanged))]
    public NetworkBool IsGameWon { get; set; }

    // Logic to track count (Server side only)
    private int _playersInZoneCount = 0;

    public override void Spawned()
    {
        // Ensure UI is hidden at start
        if (VictoryScreenObject != null)
        {
            VictoryScreenObject.SetActive(false);
        }
    }

    // --- Physics Detection (Runs on Host/Server) ---

    private void OnTriggerEnter(Collider other)
    {
        // Only the host calculates the win condition
        if (!Object.HasStateAuthority) return;

        // Check if the object is a Player (looks for NetworkCharacterController or Health)
        var player = other.GetComponentInParent<NetworkCharacterController>();

        if (player != null)
        {
            _playersInZoneCount++;
            CheckWinCondition();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!Object.HasStateAuthority) return;

        var player = other.GetComponentInParent<NetworkCharacterController>();

        if (player != null)
        {
            _playersInZoneCount--;
            // Prevent negative numbers just in case
            if (_playersInZoneCount < 0) _playersInZoneCount = 0;
        }
    }

    private void CheckWinCondition()
    {
        // If we have enough players and game isn't already won
        if (_playersInZoneCount >= PlayersRequired && !IsGameWon)
        {
            IsGameWon = true; // This change triggers the callback on all clients
            Debug.Log("Victory Condition Met!");
        }
    }

    // --- Visuals (Runs on All Clients) ---

    // This method is called automatically by Fusion when 'IsGameWon' changes
    void OnVictoryStateChanged()
    {
        if (IsGameWon)
        {
            ShowVictoryUI();
        }
    }

    private void ShowVictoryUI()
    {
        if (VictoryScreenObject != null)
        {
            VictoryScreenObject.SetActive(true);
        }
        else
        {
            Debug.LogError("WinZone: Victory Screen Object is not assigned!");
        }
    }
}