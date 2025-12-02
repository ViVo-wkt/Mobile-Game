using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using UnityEngine;

public class NetworkInputManager : MonoBehaviour, INetworkRunnerCallbacks
{
    public static CustomJoystick MoveJoystick;
    public static CustomJoystick AimJoystick;

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        var data = new NetworkInputData();

        // 1. Read Movement Joystick
        if (MoveJoystick != null)
        {
            data.MoveDirection = MoveJoystick.Direction;
        }
        else
        {
            // Fallback to Keyboard (for testing on PC)
            data.MoveDirection = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        }

        // 2. Read Aim/Tendril Joystick
        if (AimJoystick != null)
        {
            data.AimDirection = AimJoystick.Direction;
        }

        // 3. Send the packet
        input.Set(data);
    }

    // --- Fusion 2.0 Callback Implementation Fixes ---

    // Fusion 2 requires 'ReliableKey' in the signature
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }

    // Fusion 2 added this new method which must be implemented
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }

    // --- Standard Callbacks ---
    
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    
    // Note: If Unity complains about "Incorrect Signature" for these two, it is usually a warning
    // because the names match legacy Unity networking events. Since we implement the interface explicitly
    // by having the method present, Fusion will call these correctly.
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