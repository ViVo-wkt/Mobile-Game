using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSpawner : NetworkBehaviour, INetworkRunnerCallbacks
{
    public NetworkPrefabRef PlayerPrefab;

    // Keep track of players we've already spawned for in this session locally
    private HashSet<PlayerRef> _spawnedPlayers = new HashSet<PlayerRef>();

    public override void Spawned()
    {
        // Only the Master Client (State Authority) spawns players in Shared Mode
        if (Runner.IsSharedModeMasterClient || Runner.IsServer)
        {
            foreach (var player in Runner.ActivePlayers)
            {
                SpawnPlayer(player);
            }
        }
        
        // Register callbacks to listen for future join events
        // Note: We implement the full INetworkRunnerCallbacks interface below
        Runner.AddCallbacks(this);
    }
    
    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        _spawnedPlayers.Clear();
        runner.RemoveCallbacks(this);
    }

    // Required by INetworkRunnerCallbacks - Triggered when a new player joins
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (runner.IsSharedModeMasterClient || runner.IsServer)
        {
            SpawnPlayer(player);
        }
    }

    private void SpawnPlayer(PlayerRef player)
    {
        // 1. Check if we already processed this player locally
        if (_spawnedPlayers.Contains(player)) return;

        // 2. Check if Fusion already has an object for this player (Re-join protection)
        if (Runner.GetPlayerObject(player) != null)
        {
            _spawnedPlayers.Add(player);
            return;
        }

        _spawnedPlayers.Add(player);

        Debug.Log($"[PlayerSpawner] Spawning character for Player: {player}");

        // FIX: Increased Y from 1 to 2 to prevent spawning inside the floor.
        // Gravity will pull the player down safely.
        Vector3 spawnPosition = new Vector3(0, 2f, 0);
        
        try 
        {
            NetworkObject playerObject = Runner.Spawn(
                PlayerPrefab,
                spawnPosition,
                Quaternion.identity,
                inputAuthority: player 
            );

            Runner.SetPlayerObject(player, playerObject);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PlayerSpawner] Failed to spawn player! Exception: {e.Message}");
            _spawnedPlayers.Remove(player);
        }
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) 
    {
        if (_spawnedPlayers.Contains(player)) _spawnedPlayers.Remove(player);
    }

    // --- INetworkRunnerCallbacks Empty Boilerplate ---
    // These methods are required to satisfy the interface contract
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
}