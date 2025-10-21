using Fusion;
using UnityEngine;

public class PlayerSpawner : SimulationBehaviour, IPlayerJoined
{
    public GameObject PlayerPrefab;

    public void PlayerJoined(PlayerRef player)
    {
        // 1. Only the client responsible for spawning should execute this logic.
        // In Shared Mode, every client runs this, but we only want to spawn the
        // character when that character's specific player joins.

        // This is a common pattern for local-only spawning in Shared Mode,
        // but it doesn't correctly handle network synchronization in all cases.
        // We will use the Runner.IsSharedModeMasterClient check for robustness.

        if (Runner.IsSharedModeMasterClient || Runner.IsServer) // Use MasterClient/Server check for authoritative spawning
        {
            // The position where the player will spawn
            Vector3 spawnPosition = new Vector3(0, 1, 0);

            // 2. CRITICAL FIX: The Runner.Spawn() call must pass the 'player' reference
            //    as the 'inputAuthority' argument.
            NetworkObject playerObject = Runner.Spawn(
                prefab: PlayerPrefab,
                position: spawnPosition,
                rotation: Quaternion.identity,
                inputAuthority: player // <--- THIS ASSIGNS INPUT AUTHORITY
            );

            // Optional but recommended: Link the NetworkObject to the PlayerRef
            Runner.SetPlayerObject(player, playerObject);
        }
    }
}