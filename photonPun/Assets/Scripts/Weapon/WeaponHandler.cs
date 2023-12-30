using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using System;

public class WeaponHandler : NetworkBehaviour
{
    [Header("Prefabs")]
    public GranadeHandler granadePrefab;
    public RocketHandler rocketPrefab;

    [Header("Effects")]
    public ParticleSystem fireParticleSystem;

    [Header("Aim")]
    public Transform aimPoint;

    [Header("Collision")]
    public LayerMask collisionLayers;

    //Network
    [Networked] public NetworkButtons ButtonsPreviousTwo { get; set; }
    [Networked(OnChanged = nameof(OnFireChanged))] public bool isFiring { get; set; }

    float lastTimeFired = 0;

    //Timing
    TickTimer granadeFireDelay = TickTimer.None;
    TickTimer rocketFireDelay = TickTimer.None;

    //Other Components
    HPHandler hpHandler;
    NetworkPlayer networkPlayer;
    NetworkObject networkObject;

    private void Awake()
    {
        hpHandler = GetComponent<HPHandler>();
        networkPlayer = GetComponent<NetworkPlayer>();
        networkObject = GetComponent<NetworkObject>();
    }

    public override void FixedUpdateNetwork()
    {
        if (hpHandler.isDead)
        {
            return;
        }

        if (GetInput(out NetworkInputData networkInputData))
        {
            NetworkButtons buttons = networkInputData.buttons;
            var pressed = buttons.GetPressed(ButtonsPreviousTwo);              //buttons.GetPressed 和上衣個按鈕做比較目前的按鈕使否被按下
            ButtonsPreviousTwo = buttons;                                      //獲取上一個被按下的按鈕

            if (pressed.IsSet(InputButtons.FIRE))
                Fire(networkInputData.aimForwardVector);

            if (pressed.IsSet(InputButtons.ThrowGrenade))
                FireGrenade(networkInputData.aimForwardVector);

            if (pressed.IsSet(InputButtons.RocketLauncherFire))
                FireRocket(networkInputData.aimForwardVector);
        }
    }

    private void Fire(Vector3 aimForwardVertor)
    {
        if (Time.time - lastTimeFired < 0.15f)
            return;

        StartCoroutine(FireEffect());

        Runner.LagCompensation.Raycast(aimPoint.position, aimForwardVertor, 100, Object.InputAuthority, out var hitInfo, collisionLayers, HitOptions.IgnoreInputAuthority);

        float hitDistance = 100;
        bool isHitOtherPlayer = false;

        if (hitInfo.Distance > 0)
            hitDistance = hitInfo.Distance;

        if (hitInfo.Hitbox != null)
        {
            Debug.Log($"{Time.time}{transform.name} hit hitbox {hitInfo.Hitbox.transform.root.name}");

            if (Object.HasStateAuthority)
                hitInfo.Hitbox.transform.root.GetComponent<HPHandler>().OnTakeDamage(networkPlayer.nickName.ToString(), 1);

            isHitOtherPlayer = true;
        }
        else if (hitInfo.Collider != null)
        {
            Debug.Log($"{Time.time}{transform.name} hit PhysX collider {hitInfo.Hitbox.transform.root.name}");

            // isHitOtherPlayer = true;
        }

        //Debug
        if (isHitOtherPlayer)
        {
            Debug.DrawRay(aimPoint.position, aimForwardVertor * hitDistance, Color.red, 1);
        }
        else
        {
            Debug.DrawRay(aimPoint.position, aimForwardVertor * hitDistance, Color.green, 1);
        }

        lastTimeFired = Time.time;
    }

    private IEnumerator FireEffect()
    {
        isFiring = true;
        fireParticleSystem.Play();

        yield return new WaitForSeconds(0.09f);


        isFiring = false;
    }

    public static void OnFireChanged(Changed<WeaponHandler> changed)
    {
        Debug.Log($"{Time.time} OnFireChanged value {changed.Behaviour.isFiring}");

        bool isFiringCurrent = changed.Behaviour.isFiring;

        //Load the old value
        changed.LoadOld();

        bool isFiringOld = changed.Behaviour.isFiring;

        if (isFiringCurrent && !isFiringOld)
        {
            changed.Behaviour.OnFireRemote();
        }
    }

    private void OnFireRemote()
    {
        if (!Object.HasInputAuthority) { }
        // fireParticleSystem.Play();
    }


    private void FireGrenade(Vector3 aimForwardVector)
    {
        if (granadeFireDelay.ExpiredOrNotRunning(Runner))
        {
            Runner.Spawn(granadePrefab, aimPoint.position + aimForwardVector * 1.5f, Quaternion.LookRotation(aimForwardVector), Object.InputAuthority, (runner, spawnedGrenade) =>
            {
                spawnedGrenade.GetComponent<GranadeHandler>().Throw(aimForwardVector * 15, Object.InputAuthority, networkPlayer.nickName.ToString());
            });

            //Start a new timer to avoid granade spamming
            granadeFireDelay = TickTimer.CreateFromSeconds(Runner, 1.0f);
        }
    }

    private void FireRocket(Vector3 aimForwardVector)
    {
        if (rocketFireDelay.ExpiredOrNotRunning(Runner))
        {
            Runner.Spawn(rocketPrefab, aimPoint.position + aimForwardVector * 1.5f, Quaternion.LookRotation(aimForwardVector), Object.InputAuthority, (runner, spawnedRocket) =>
            {
                spawnedRocket.GetComponent<RocketHandler>().Fire(Object.InputAuthority, networkObject, networkPlayer.nickName.ToString());
            });

            //Start a new timer to avoid granade spamming
            rocketFireDelay = TickTimer.CreateFromSeconds(Runner, 3.0f);
        }
    }
}