using Fusion;
using UnityEngine;
using System.Collections.Generic;

public class PlayerSpawner : NetworkBehaviour, IPlayerJoined
{
    public NetworkPrefabRef PlayerPrefab;

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
    }

    public void PlayerJoined(PlayerRef player)
    {
        if (Runner.IsSharedModeMasterClient || Runner.IsServer)
        {
            SpawnPlayer(player);
        }
    }

    private void SpawnPlayer(PlayerRef player)
    {
        // Safety Check: Prevent duplicate spawning
        if (Runner.GetPlayerObject(player) != null)
        {
            return;
        }

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
        }
    }
}