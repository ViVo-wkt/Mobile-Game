using Fusion;
using UnityEngine;

public struct NetworkInputData : INetworkInput
{
    public Vector2 MoveDirection;
    public Vector2 AimDirection;
    public NetworkBool AbilityTriggered; // NEW: Tracks button press
}