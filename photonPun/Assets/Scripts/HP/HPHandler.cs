using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Fusion;
using System;

public class HPHandler : NetworkBehaviour
{
    [Networked(OnChanged = nameof(OnHPChanged))] byte HP { get; set; }
    [Networked(OnChanged = nameof(OnStateChanged))] public bool isDead { get; set; }

    bool isInitialized = false;
    const byte startHp = 5;

    [Header("UIOnHitDamage")]
    public Color uiOnHitColor;
    public Image uiOnHitImage;
    public MeshRenderer bodyMeshRender;
    private Color defaultMeshBodyColor;


    public GameObject playerModel;
    public GameObject deathGameObjectPrefab;

    public bool skipSettingStartValues = false;

    //Other Component
    HitboxRoot hitboxRoot;
    CharacterMovementHandler characterMovementHandler;
    NetworkInGameMessages networkInGameMessages;
    NetworkPlayer networkPlayer;

    private void Awake()
    {
        hitboxRoot = GetComponentInChildren<HitboxRoot>();
        characterMovementHandler = GetComponent<CharacterMovementHandler>();
        networkInGameMessages = GetComponent<NetworkInGameMessages>();
        networkPlayer = GetComponent<NetworkPlayer>();
    }

    private void Start()
    {
        if (!skipSettingStartValues)
        {
            HP = startHp;
            isDead = false;
        }

        // defaultMeshBodyColor = bodyMeshRender.material.color;

        isInitialized = true;
    }

    public static void OnHPChanged(Changed<HPHandler> changed)
    {
        Debug.Log($"{Time.time} OnHpChanged value {changed.Behaviour.HP}");

        byte newHp = changed.Behaviour.HP;

        changed.LoadOld();

        byte oldHp = changed.Behaviour.HP;

        //Check if the HP has been decreased or increased
        if (newHp < oldHp)
            changed.Behaviour.OnHpReduced();
    }

    public static void OnStateChanged(Changed<HPHandler> changed)
    {
        Debug.Log($"{Time.time} OnHpChanged value {changed.Behaviour.isDead}");

        bool isDeadCurrent = changed.Behaviour.isDead;

        changed.LoadOld();

        bool isDeadOld = changed.Behaviour.isDead;

        if (isDeadCurrent)
            changed.Behaviour.OnDeath();
        else if (!isDeadCurrent && isDeadOld)
            changed.Behaviour.OnRevive();
    }

    //Function is only called on the server
    public void OnTakeDamage(string damageCausedByPlayerNickName, byte damageAmount)
    {
        //Only take damage while alive
        if (isDead)
            return;

        if (damageAmount >= HP)
            damageAmount = HP;

        HP -= damageAmount;

        Debug.Log($"{Time.time} {transform.name} took damage got {HP} left");

        if (HP <= 0)
        {
            Debug.Log($"{Time.time} {transform.name} died");
            networkInGameMessages.SendInGameRPCMessage(damageCausedByPlayerNickName, $"Killed <b>{networkPlayer.nickName.ToString()}</b>");

            StartCoroutine(ServerRevieveCO());
            isDead = true;
        }
    }

    public void OnRespawned()
    {
        //Reset variable
        HP = startHp;
        isDead = false;
    }

    private IEnumerator OnHitCO()
    {
        bodyMeshRender.material.color = Color.white;

        if (Object.HasInputAuthority)
            uiOnHitImage.color = uiOnHitColor;

        yield return new WaitForSeconds(0.2f);

        bodyMeshRender.material.color = defaultMeshBodyColor;

        if (Object.HasInputAuthority && !isDead)
            uiOnHitImage.color = new Color(0, 0, 0, 0);
    }

    private IEnumerator ServerRevieveCO()
    {
        yield return new WaitForSeconds(2.0f);

        characterMovementHandler.RequestRespawn();
    }

    private void OnHpReduced()
    {
        if (!isInitialized)
            return;

        StartCoroutine(OnHitCO());
    }

    private void OnDeath()
    {
        Debug.Log($"{Time.time} OnDeath");

        playerModel.gameObject.SetActive(false);
        hitboxRoot.HitboxRootActive = false;
        characterMovementHandler.SetCharacterControllerEnabled(false);

        Instantiate(deathGameObjectPrefab, transform.position, Quaternion.identity);
    }

    private void OnRevive()
    {
        Debug.Log($"{Time.time} OnRevive");

        if (Object.HasInputAuthority)
            uiOnHitImage.color = new Color(0, 0, 0, 0);

        playerModel.gameObject.SetActive(true);
        hitboxRoot.HitboxRootActive = true;
        characterMovementHandler.SetCharacterControllerEnabled(true);
    }
}
