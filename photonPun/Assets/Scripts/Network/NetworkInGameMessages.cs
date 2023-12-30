using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class NetworkInGameMessages : NetworkBehaviour
{
    InGameMessageUIHandler InGameMessageUIHandler;


    public void SendInGameRPCMessage(string userNickName, string message)
    {
        RPC_InGameMessage($"<b>{userNickName}</b> {message}");
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_InGameMessage(string message, RpcInfo info = default)
    {
        Debug.Log($"[RPC] InGameMessage {message}");

        if (InGameMessageUIHandler == null)
            InGameMessageUIHandler = NetworkPlayer.Local.localCameraHandler.GetComponentInChildren<InGameMessageUIHandler>();

        if (InGameMessageUIHandler != null)
            InGameMessageUIHandler.OnGameMessageReceived(message);
    }
}
