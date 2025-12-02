using Fusion;
using UnityEngine;

public struct NetworkInputData : INetworkInput
{
    // We use Vector2 for bandwidth efficiency (Fusion compresses these automatically)
    public Vector2 MoveDirection;
    public Vector2 AimDirection;
}