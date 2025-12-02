using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using UnityEngine;

public class NetworkInputManager : MonoBehaviour, INetworkRunnerCallbacks
{
    // DICTIONARY: Maps a specific NetworkRunner to its specific Joysticks
    // This allows multiple players to run in the Editor without overwriting each other's input.
    private static Dictionary<NetworkRunner, (CustomJoystick Move, CustomJoystick Aim)> _localInputs =
        new Dictionary<NetworkRunner, (CustomJoystick, CustomJoystick)>();

    // Call this from PlayerMovement to register controls for a specific runner
    public static void RegisterInput(NetworkRunner runner, CustomJoystick move, CustomJoystick aim)
    {
        if (runner == null) return;

        if (_localInputs.ContainsKey(runner))
        {
            _localInputs[runner] = (move, aim);
        }
        else
        {
            _localInputs.Add(runner, (move, aim));
        }
    }

    public static void UnregisterInput(NetworkRunner runner)
    {
        if (runner != null && _localInputs.ContainsKey(runner))
        {
            _localInputs.Remove(runner);
        }
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        var data = new NetworkInputData();

        // Check if we have registered inputs for THIS specific runner
        if (_localInputs.TryGetValue(runner, out var joysticks))
        {
            // 1. Read Movement
            if (joysticks.Move != null)
                data.MoveDirection = joysticks.Move.Direction;

            // 2. Read Aim
            if (joysticks.Aim != null)
                data.AimDirection = joysticks.Aim.Direction;
        }
        else
        {
            // Fallback for debugging (e.g. if UI failed to spawn)
            // Note: This applies to ALL runners if they have no UI, so be careful in Editor
            data.MoveDirection = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        }

        input.Set(data);
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        UnregisterInput(runner);
    }

    // --- Boilerplate Callbacks ---
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
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