using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using TMPro;

public class NetworkPlayer : NetworkBehaviour
{
    public static NetworkPlayer Local { get; set; }
    public Transform playerModel;
    public TextMeshProUGUI playerNickNameTM;
    [Networked(OnChanged = nameof(OnNickNameChanged))] public NetworkString<_16> nickName { get; set; }
    [Networked] public int token { get; set; }  //Remote Client Token Hash

    private bool isPublicJoinMessageSent = false;
    public LocalCameraHandler localCameraHandler;
    public GameObject localUI;

    //Other component
    NetworkInGameMessages networkInGameMessages;

    private void Awake()
    {
        networkInGameMessages = GetComponent<NetworkInGameMessages>();
    }

    public override void Spawned()
    {
        if (Object.HasInputAuthority)
        {
            Local = this;

            //Set the layer of the local player model
            Utils.SetRenderLayerInChildren(playerModel, LayerMask.NameToLayer("LocalPlayerLayer"));

            //Disable main camera
            if (Camera.main != null)
                Camera.main.gameObject.SetActive(false);

            //Enable 1 audio listner
            AudioListener audioListener = GetComponentInChildren<AudioListener>(true);
            audioListener.enabled = true;

            //Enable the local camera
            localCameraHandler.localCamera.enabled = true;

            //Detach camera if enabled
            localCameraHandler.transform.parent = null;

            //Enable UI for local player
            localUI.SetActive(true);

            Rpc_SetNickName(GameManager.Instance.playerNickName);

            Debug.Log("Spawned local Player");
        }
        else
        {
            //Disable the local camera for remote player
            localCameraHandler.localCamera.enabled = false;

            //Disable UI for remote player
            localUI.SetActive(false);

            // Only 1 audio listener is allowed in the scene so disable remote players audio listner
            AudioListener audioListener = GetComponentInChildren<AudioListener>();
            audioListener.enabled = false;

            Debug.Log("Spawned remote Player");
        }

        //Set the player as a player object
        Runner.SetPlayerObject(Object.InputAuthority, Object);

        //Make it easier to tell which player in
        transform.name = $"P_{Object.Id}";
    }

    public void PlayerLeft(PlayerRef player)
    {
        if (Object.HasStateAuthority)
        {
            if (Runner.TryGetPlayerObject(player, out NetworkObject playerNetworkObject))
            {
                if (playerNetworkObject == Object)
                    Local.GetComponent<NetworkInGameMessages>().SendInGameRPCMessage(playerNetworkObject.GetComponent<NetworkPlayer>().nickName.ToString(), "left");

            }
        }
    }

    private static void OnNickNameChanged(Changed<NetworkPlayer> changed)
    {
        Debug.Log($"{Time.time} OnChanged value {changed.Behaviour.nickName}");

        changed.Behaviour.OnNickNameChanged();
    }

    private void OnNickNameChanged()
    {
        Debug.Log($"Nickname changed for player to {nickName} for player {gameObject.name}");

        playerNickNameTM.text = nickName.ToString();
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void Rpc_SetNickName(string nickName, RpcInfo info = default)
    {
        Debug.Log($"[RPC] SetNickName {nickName}");
        this.nickName = nickName;

        if (!isPublicJoinMessageSent)
        {
            networkInGameMessages.SendInGameRPCMessage(nickName, "Joined");

            isPublicJoinMessageSent = true;
        }
    }

    private void OnDestroy()
    {
        if (localCameraHandler != null)
            Destroy(localCameraHandler.gameObject);
    }
}

