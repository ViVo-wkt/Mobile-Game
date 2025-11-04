using Fusion;
using UnityEngine;

public class TendrilLauncher : NetworkBehaviour
{
    public NetworkPrefabRef TendrilPrefab;
    public float MaxTendrilRange = 10f;
    public LayerMask TendrilRaycastMask;

    [Networked] private NetworkObject ActiveTendril { get; set; }
    [Networked] private Vector3 AimTargetPoint { get; set; }
    [Networked] private bool IsRetracting { get; set; }

    private void Awake()
    {
        TendrilRaycastMask = ~LayerMask.GetMask("Player");
    }

    void Update()
    {
        if (!HasInputAuthority) return;

        // FIXED: STRICT BLOCK - No extend if retracting OR no target
        bool wantsToExtend = !IsRetracting && AimTargetPoint != Vector3.zero;
        bool hasTendril = ActiveTendril != null;

        if (wantsToExtend && !hasTendril)
        {
            SpawnTendril(AimTargetPoint);
        }
        else if (wantsToExtend && hasTendril)
        {
            ActiveTendril.GetComponent<TendrilController>().SetTarget(AimTargetPoint, MaxTendrilRange);
        }
        else if (!wantsToExtend && hasTendril)
        {
            ActiveTendril.GetComponent<TendrilController>().Retract();
            IsRetracting = true;
            AimTargetPoint = Vector3.zero; // ← FORCE CLEAR IMMEDIATELY
        }
    }

    private void SpawnTendril(Vector3 initialTarget)
    {
        if (!TendrilPrefab.IsValid) return;

        Vector3 spawnPos = transform.position;
        Vector3 dir = (initialTarget - spawnPos).normalized;
        Quaternion rot = Quaternion.LookRotation(Vector3.up, dir);

        NetworkObject tendril = Runner.Spawn(
            TendrilPrefab, spawnPos, rot,
            inputAuthority: Object.InputAuthority,
            (runner, obj) => obj.GetComponent<TendrilController>().Initialize(this, initialTarget)
        );

        ActiveTendril = tendril;
        IsRetracting = false;
    }

    public void SetAimTarget(Vector3 target)
    {
        AimTargetPoint = target;
        if (target != Vector3.zero)
        {
            IsRetracting = false; // Allow extend
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void ClearActiveTendrilRpc()
    {
        ActiveTendril = null;
        IsRetracting = false;
        AimTargetPoint = Vector3.zero; // ← DOUBLE CLEAR
    }
}