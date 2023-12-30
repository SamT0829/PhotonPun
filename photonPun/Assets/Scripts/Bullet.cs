using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class Bullet : NetworkBehaviour
{
    [Networked]
    private TickTimer life { get; set; }     //已 photon tick 為主的計時器

    [SerializeField]
    private float bulletSpeed = 5f;

    //NetworkBehaviour 的 start
    public override void Spawned()
    {
        life = TickTimer.CreateFromSeconds(Runner, 5.0f);       //Runner 因使用NetworkBehaviour 會自動找出場境裡的 NetworkRunner
    }

    public override void FixedUpdateNetwork()
    {
        if (life.Expired(Runner))           //判斷life是否歸零
        {
            Runner.Despawn(Object);
        }
        else
            transform.position += bulletSpeed * transform.forward * Runner.DeltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            var player = other.GetComponent<CharacterMovementHandler>();
            // player.TakeDamage(10);

            Runner.Despawn(Object);
        }
    }
}
