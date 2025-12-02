using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using UnityEngine;

public class NetworkInputManager : MonoBehaviour, INetworkRunnerCallbacks
{
    // Dictionary to map each Runner (Player) to their specific joysticks
    private static Dictionary<NetworkRunner, (CustomJoystick Move, CustomJoystick Aim)> _joysticks =
        new Dictionary<NetworkRunner, (CustomJoystick Move, CustomJoystick Aim)>();

    // Called by PlayerMovement to register its local joysticks
    public static void RegisterInput(NetworkRunner runner, CustomJoystick move, CustomJoystick aim)
    {
        if (runner == null) return;

        if (_joysticks.ContainsKey(runner))
            _joysticks[runner] = (move, aim);
        else
            _joysticks.Add(runner, (move, aim));
    }

    public static void UnregisterInput(NetworkRunner runner)
    {
        if (runner != null && _joysticks.ContainsKey(runner))
            _joysticks.Remove(runner);
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        var data = new NetworkInputData();

        // Check if we have joysticks registered for THIS specific runner
        if (_joysticks.TryGetValue(runner, out var joysticks))
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
            // Fallback to Keyboard (Only if no joystick registered)
            data.MoveDirection = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        }

        input.Set(data);
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        UnregisterInput(runner);
    }

    // --- Boilerplate ---
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
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
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
}