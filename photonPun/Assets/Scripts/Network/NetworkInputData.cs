using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public struct NetworkInputData : INetworkInput  //photon input 實現接口
{
    public NetworkButtons buttons;
    public Vector2 movementInput;
    public Vector3 aimForwardVector;
}

public enum InputButtons
{
    JUMP,
    FIRE,
    ThrowGrenade,
    RocketLauncherFire,
}
