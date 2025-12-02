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
        // Register callbacks
        Runner.AddCallbacks(this);
    }
    
    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        // Clean up cache and callbacks when the spawner is destroyed
        _spawnedPlayers.Clear();
        runner.RemoveCallbacks(this);
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (runner.IsSharedModeMasterClient || runner.IsServer)
        {
            SpawnPlayer(player);
        }
    }

    private void SpawnPlayer(PlayerRef player)
    {
        // 1. Local Safety Check: Prevent double-execution in the same frame/session
        if (_spawnedPlayers.Contains(player)) return;

        // 2. Fusion Safety Check: Prevent spawning if Fusion already knows about an object for this player
        if (Runner.GetPlayerObject(player) != null)
        {
            _spawnedPlayers.Add(player);
            return;
        }

        // Mark as processed immediately
        _spawnedPlayers.Add(player);

        Debug.Log($"[PlayerSpawner] Spawning character for Player: {player}");

        Vector3 spawnPosition = new Vector3(0, 1, 0);
        
        try 
        {
            // CRITICAL: The 'inputAuthority' parameter must be the 'player' we are spawning for.
            NetworkObject playerObject = Runner.Spawn(
                PlayerPrefab,
                spawnPosition,
                Quaternion.identity,
                inputAuthority: player 
            );

            // Register object so we can find it later
            Runner.SetPlayerObject(player, playerObject);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PlayerSpawner] Failed to spawn player! Exception: {e.Message}");
            // If spawn actually failed, allow retrying
            _spawnedPlayers.Remove(player);
        }
    }

    // --- INetworkRunnerCallbacks Boilerplate ---
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) 
    {
        // Optional: Despawn player object if they leave
        if (_spawnedPlayers.Contains(player)) _spawnedPlayers.Remove(player);
    }
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