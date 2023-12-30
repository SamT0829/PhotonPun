using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class GranadeHandler : NetworkBehaviour
{
    [Header("Prefab")]
    public GameObject explosionParticleSystemPrefab;

    [Header("Collision detection")]
    public LayerMask collisionLayers;

    //Throw by info
    PlayerRef throwByPlayerRef;
    string throwByPlayerName;

    //Timing
    TickTimer explodeTickTimer = TickTimer.None;

    //Hit info
    List<LagCompensatedHit> hits = new List<LagCompensatedHit>();

    //Other components
    NetworkObject networkObject;
    NetworkRigidbody networkRigidbody;

    public void Throw(Vector3 throwForce, PlayerRef throwByPlayerRef, string throwByPlayerName)
    {
        networkObject = GetComponent<NetworkObject>();
        networkRigidbody = GetComponent<NetworkRigidbody>();

        networkRigidbody.Rigidbody.AddForce(throwForce, ForceMode.Impulse);

        this.throwByPlayerRef = throwByPlayerRef;
        this.throwByPlayerName = throwByPlayerName;

        explodeTickTimer = TickTimer.CreateFromSeconds(Runner, 2f);
    }

    //Network Update
    public override void FixedUpdateNetwork()
    {
        if (Object.HasStateAuthority)
        {
            if (explodeTickTimer.Expired(Runner))
            {
                int hitCount = Runner.LagCompensation.OverlapSphere(transform.position, 4, throwByPlayerRef, hits, collisionLayers);

                for (int i = 0; i < hitCount; i++)
                {
                    HPHandler hpHandler = hits[i].Hitbox.transform.root.GetComponent<HPHandler>();

                    if (hpHandler != null)
                    {
                        hpHandler.OnTakeDamage(throwByPlayerName, 100);
                    }
                }


                Runner.Despawn(networkObject);

                //Stop the explode timer from being triggered again
                explodeTickTimer = TickTimer.None;
            }
        }
    }

    //When despawning the object we want to create a visual explosion
    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        MeshRenderer granadeMesh = GetComponentInChildren<MeshRenderer>();

        Instantiate(explosionParticleSystemPrefab, granadeMesh.transform.position, Quaternion.identity);
    }
}
