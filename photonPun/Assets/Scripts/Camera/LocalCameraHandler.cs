using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LocalCameraHandler : MonoBehaviour
{
    public Transform cameraAnchorPoint;
    //Input
    Vector2 viewInput;

    //Rotation
    float cameraRotationX;
    float cameraRotationY;

    //Other components
    NetworkCharacterControllerPrototypeCostum networkCharacterControllerPrototypeCostum;
    public Camera localCamera;

    private void Awake()
    {
        localCamera = GetComponent<Camera>();
        networkCharacterControllerPrototypeCostum = GetComponentInParent<NetworkCharacterControllerPrototypeCostum>();
    }

    private void Start()
    {
        cameraRotationX = GameManager.Instance.cameraViewRotation.x;
        cameraRotationY = GameManager.Instance.cameraViewRotation.y;
    }

    private void LateUpdate()
    {
        if (cameraAnchorPoint == null)
            return;

        if (!localCamera.enabled)
            return;

        //Move the camera to the posotion of the player
        localCamera.transform.position = cameraAnchorPoint.position;

        //Calculate rotation
        cameraRotationX += viewInput.y * Time.deltaTime * networkCharacterControllerPrototypeCostum.viewUpDownRotationSpeed;
        cameraRotationX = Mathf.Clamp(cameraRotationX, -90, 90);

        cameraRotationY += viewInput.x * Time.deltaTime * networkCharacterControllerPrototypeCostum.rotationSpeed;

        //Apply rotation
        localCamera.transform.rotation = Quaternion.Euler(cameraRotationX, cameraRotationY, 0);
    }

    public void SetViewInputVector(Vector2 viewInput)
    {
        this.viewInput = viewInput;
    }

    private void OnDestroy()
    {
        if (cameraRotationX != 0 && cameraRotationY != 0)
        {
            GameManager.Instance.cameraViewRotation.x = cameraRotationX;
            GameManager.Instance.cameraViewRotation.y = cameraRotationY;
        }
    }
}
