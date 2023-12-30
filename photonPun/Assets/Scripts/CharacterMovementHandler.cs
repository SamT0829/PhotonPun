using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Fusion;

public class CharacterMovementHandler : NetworkBehaviour
{
    private bool isRespawnRequsted = false;

    //Other Components
    private NetworkCharacterControllerPrototypeCostum networkCharacterControllerPrototypeCustom = null;      //網路實現玩家物件class
    private HPHandler hpHandler;
    private NetworkInGameMessages networkInGameMessages;
    private NetworkPlayer networkPlayer;


    // [SerializeField] private Bullet bulletPrefab;
    // [SerializeField] private Image hpBar = null;
    // [SerializeField] private MeshRenderer meshRenderer;                          //material 顏色 
    // [SerializeField] private int maxHp = 100;

    // //OnChanged  每當Hp 改變時 會呼叫 OnHpChanged 方法
    // [Networked(OnChanged = nameof(OnHpChanged))]                //[Networked]  當想要在網路進行同步 必須為{ get; set; }
    // public int Hp { get; set; }

    //[Networked]  當想要在網路進行同步 必須為{ get; set; }
    [Networked] public NetworkButtons ButtonsPrevious { get; set; }

    private void Awake()
    {
        networkCharacterControllerPrototypeCustom = GetComponent<NetworkCharacterControllerPrototypeCostum>();
        hpHandler = GetComponent<HPHandler>();
        networkInGameMessages = GetComponent<NetworkInGameMessages>();
        networkPlayer = GetComponent<NetworkPlayer>();
    }

    // public override void Spawned()
    // {
    //     if (Object.HasStateAuthority)                           //HasStateAuthority 當有私服器時才會運行
    //         Hp = maxHp;
    // }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // private void Update()
    // {
    //     if (Input.GetKeyDown(KeyCode.R))
    //     {
    //         ChangeColor_RPC(Color.red);
    //     }
    //     if (Input.GetKeyDown(KeyCode.G))
    //     {
    //         ChangeColor_RPC(Color.green);
    //     }
    //     if (Input.GetKeyDown(KeyCode.B))
    //     {
    //         ChangeColor_RPC(Color.blue);
    //     }
    // }

    public override void FixedUpdateNetwork()
    {
        if (Object.HasStateAuthority)
        {
            if (isRespawnRequsted)
            {
                Respawn();
                return;
            }

            //Don't update the client position when they are death
            if (hpHandler.isDead)
                return;
        }

        if (GetInput(out NetworkInputData networkInputData))                //GetInput(out INetworkInput) 接收 INetworkInput 方法
        {
            NetworkButtons buttons = networkInputData.buttons;
            var pressed = buttons.GetPressed(ButtonsPrevious);              //buttons.GetPressed 和上衣個按鈕做比較目前的按鈕使否被按下
            ButtonsPrevious = buttons;                                      //獲取上一個被按下的按鈕

            //Rotate the transform according to the Client aim vector
            transform.forward = networkInputData.aimForwardVector;

            //Cancel out rotation on X axis as we don't want our character to tilt
            Quaternion rotation = transform.rotation;
            rotation.eulerAngles = new Vector3(0, rotation.eulerAngles.y, 0);
            transform.rotation = rotation;

            //Move            
            // Vector2 moveVector = data.movementInput.normalized;      //for 上下走動
            //Runner.DeltaTime Photon tick 時時間 (Time.FixedDeltaTime) 
            Vector3 moveDirection = transform.forward * networkInputData.movementInput.y + transform.right * networkInputData.movementInput.x;
            moveDirection.Normalize();
            if (networkCharacterControllerPrototypeCustom.Controller.enabled)
                networkCharacterControllerPrototypeCustom.Move(moveDirection * Runner.DeltaTime);

            //Jump
            if (pressed.IsSet(InputButtons.JUMP))
            {
                networkCharacterControllerPrototypeCustom.Jump();
            }

            // //Fire
            // if (pressed.IsSet(InputButtons.FIRE))
            // {
            //     PlayerFire(networkInputData.aimForwardVector);
            // }
        }

        CheckFallRespawn();
    }

    // private void PlayerFire(Vector3 target)
    // {
    //     // Runner.Spawn(bulletPrefab, transform.position + transform.TransformDirection(Vector3.forward),
    //     //     Quaternion.LookRotation(transform.TransformDirection(Vector3.forward)), Object.InputAuthority); //PLayer 物件

    //     Runner.Spawn(bulletPrefab, transform.position + target, Quaternion.LookRotation(target), Object.InputAuthority); //PLayer 物件
    // }

    public void RequestRespawn()
    {
        isRespawnRequsted = true;
    }

    private void CheckFallRespawn()
    {
        if (networkCharacterControllerPrototypeCustom.transform.position.y <= -12f)
        {
            if (Object.HasStateAuthority)
            {
                Debug.Log($"{Time.time} Respawn due to fall outside of map at position {transform.position}");

                networkInGameMessages.SendInGameRPCMessage(networkPlayer.nickName.ToString(), "fall off the world");
                Respawn();
            }
            // networkCharacterControllerPrototypeCustom.transform.position = Vector3.up * 2;
            // Hp = maxHp;
        }
    }

    private void Respawn()
    {
        // networkCharacterControllerPrototypeCustom.TeleportToPosition(Utils.GetRandomSpawnPoin());
        networkCharacterControllerPrototypeCustom.TeleportToPosition(Vector3.up * 2);

        hpHandler.OnRespawned();

        isRespawnRequsted = false;
    }

    // public void TakeDamage(int damage)
    // {
    //     if (Object.HasStateAuthority)
    //     {
    //         Hp -= damage;
    //     }
    // }

    // //Changed 代表變化後的值
    // private static void OnHpChanged(Changed<CharacterMovementHandler> changed)
    // {
    //     changed.Behaviour.hpBar.fillAmount = (float)changed.Behaviour.Hp / changed.Behaviour.maxHp;
    // }

    // //更重要的是，它們不是網路狀態的一部分，所以任何在RPC發送後連接或重新連接的玩家，或者只是因為它被不可靠地發送而沒有收到它，都不會看到它的後果。
    // [Rpc(RpcSources.InputAuthority, RpcTargets.All)]        //RpcSources Rpc來源  RpcTargets Rpc收到的目標
    // private void ChangeColor_RPC(Color newColor)
    // {
    //     meshRenderer.material.color = newColor;
    // }

    public void SetCharacterControllerEnabled(bool isEnable)
    {
        networkCharacterControllerPrototypeCustom.Controller.enabled = isEnable;
    }
}
