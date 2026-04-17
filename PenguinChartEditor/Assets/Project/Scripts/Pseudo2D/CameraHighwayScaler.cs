using System;
using UnityEngine;

public class CameraHighwayScaler : MonoBehaviour
{
    [SerializeField] private Camera orthographicSceneCamera;
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
        Highway3D.highwayLength = (22.5f / 9.0f) * cameraSize;
    }

    private void OnDestroy()
    {
        Highway3D.highwayLength = UserSettings.userSetHighwayLength;
    }
}