using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterInputHandler : MonoBehaviour
{
    Vector2 moveInputVector = Vector2.zero;
    Vector2 viewInputVector = Vector2.zero;
    bool isJumpButtonPressed;
    bool isFireButtonPressed;
    bool isThrowGranadeButtonPressed;
    bool isRocketLauncherFireButtonPressed;

    //Other components
    LocalCameraHandler localCameraHandler;
    CharacterMovementHandler characterInputHandler;

    private void Awake()
    {
        localCameraHandler = GetComponentInChildren<LocalCameraHandler>();
        characterInputHandler = GetComponent<CharacterMovementHandler>();
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (!characterInputHandler.Object.HasInputAuthority)
            return;

        //View input
        viewInputVector.x = Input.GetAxis("Mouse X");
        viewInputVector.y = Input.GetAxis("Mouse Y") * -1;

        //Move input
        moveInputVector.x = Input.GetAxis("Horizontal");
        moveInputVector.y = Input.GetAxis("Vertical");

        //Button input
        isJumpButtonPressed = Input.GetButtonDown("Jump");
        isFireButtonPressed = Input.GetButtonDown("Fire1");
        isThrowGranadeButtonPressed = Input.GetKeyDown(KeyCode.G);
        isRocketLauncherFireButtonPressed = Input.GetButtonDown("Fire2");

        //SetView
        localCameraHandler.SetViewInputVector(viewInputVector);
    }

    public NetworkInputData GetNetworkInputData()
    {
        NetworkInputData networkInputData = new NetworkInputData();

        //Aim data
        networkInputData.aimForwardVector = localCameraHandler.transform.forward;

        //Move data
        networkInputData.movementInput = moveInputVector;

        //Buttons data
        networkInputData.buttons.Set(InputButtons.JUMP, isJumpButtonPressed);
        networkInputData.buttons.Set(InputButtons.FIRE, isFireButtonPressed);
        networkInputData.buttons.Set(InputButtons.ThrowGrenade, isThrowGranadeButtonPressed);
        networkInputData.buttons.Set(InputButtons.RocketLauncherFire, isRocketLauncherFireButtonPressed);

        return networkInputData;
    }
}
