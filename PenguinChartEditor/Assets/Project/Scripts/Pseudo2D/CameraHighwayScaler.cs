using System;
using System.Windows.Forms.VisualStyles;
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

    private void Start()
    {
        startingCameraSize = cameraSize;
        startingCameraZ = cameraZPosition;
        startingHighwayLength = (22.5f / 9.0f) * cameraSize;
        
        UpdateCameraConfiguration(true);
        
        PenguinSceneTabScaler.ScreenSizeUpdated += UpdateCameraConfiguration;
        instance = this;
    }

    private void OnDestroy()
    {
        Highway.highwayLength = Chart.settings.userSetHighwayLength;
        PenguinSceneTabScaler.ScreenSizeUpdated -= UpdateCameraConfiguration;
        instance = null;
    }

    private const float DEFAULT_RATIO = 16.0f / 9.0f;
    private float startingCameraSize;
    private float startingCameraZ;
    private float startingHighwayLength;
    private float previousRatio = int.MinValue;

    private void UpdateCameraConfiguration(float _) => UpdateCameraConfiguration(false);
    
    // Scales the camera linearly for screen size changes based on camera/highway settings. Since the highway is 
    // effectively a UI element, but will not scale naturally as such, this function exists to solve that.
    // This scales the highway ONLY when the window is more vertical than normal, because it is unusable when not scaled
    // when the height is greater than expected. Looks great normally in ultrawide ratios, so there is no UI scaling
    // for such cases.
    private void UpdateCameraConfiguration(bool overrideRedundancyChecks)
    {
        var newRatio = (float)Screen.width / Screen.height;

        // Overriding redundancy checks happens on initialization, when things will be initialized for a 16:9 display
        // even if the display is not actually 16:9. ratio stays below default ratio so that the highway does not become
        // oversized.
        if (!overrideRedundancyChecks)
        {
            if (newRatio == previousRatio || newRatio > DEFAULT_RATIO) return;
        }
        else
        {
            if (newRatio > DEFAULT_RATIO) newRatio = DEFAULT_RATIO;
        }
        
        // mmmm...ratio of ratios. 
        var increaseRatio = DEFAULT_RATIO / newRatio;

        cameraSize = increaseRatio * startingCameraSize;
        cameraZPosition = increaseRatio * startingCameraZ;
        Highway.highwayLength = increaseRatio * startingHighwayLength;

        previousRatio = newRatio;
    }
}