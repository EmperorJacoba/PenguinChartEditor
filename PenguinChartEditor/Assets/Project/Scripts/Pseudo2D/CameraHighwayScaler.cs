using System;
using UnityEngine;

public class CameraHighwayScaler : MonoBehaviour
{
    public static CameraHighwayScaler instance;
    [SerializeField] public Camera orthographicSceneCamera;
    public float cameraSize
    {
        get
        {
            return orthographicSceneCamera.orthographicSize;
        }
        set
        {
            orthographicSceneCamera.orthographicSize = value;
        }
    }

    public float cameraZPosition
    {
        get
        {
            return orthographicSceneCamera.transform.position.z;
        }
        set
        {
            orthographicSceneCamera.transform.position = new Vector3(
                orthographicSceneCamera.transform.position.x,
                orthographicSceneCamera.transform.position.y,
                value
            );
        }
    }
    
    private void OnEnable()
    {
        Highway.highwayLength = (22.5f / 9.0f) * cameraSize;
        instance = this;
    }

    private void OnDestroy()
    {
        Highway.highwayLength = UserSettings.userSetHighwayLength;
        instance = null;
    }
}