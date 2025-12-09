using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SceneDetails : MonoBehaviour
{
    public SceneType currentScene;
    public bool is2D = false;

    // Use ScreenReference in TempoMap, use highway GameObject in Chart tab.
    public Transform highway;

    public float HighwayLength
    {
        get
        {
            if (!is2D)
            {
                return highway.localScale.z;
            }
            var screenRef = (RectTransform)highway;
            return screenRef.rect.height;
        }
    }

    public GraphicRaycaster overlayUIRaycaster;
    public BaseRaycaster eventRaycaster;
    public PhysicsRaycaster cameraHighwayRaycaster;

    public LanePositions lanePositionReference;


    public bool IsSceneOverlayUIHit() => IsRaycasterHit(overlayUIRaycaster);
    public bool IsEventDataHit() => IsRaycasterHit(eventRaycaster);

    bool IsRaycasterHit(BaseRaycaster targetRaycaster)
    {
        PointerEventData pointerData = new(EventSystem.current)
        {
            position = Input.mousePosition
        };

        List<RaycastResult> results = new();
        targetRaycaster.Raycast(pointerData, results);

        // If a component from the toolboxes is raycasted from the cursor, then the overlay is hit.
        return results.Count > 0;
    }

    // please fit for 3D
    public Vector3 GetCursorHighwayPosition()
    {
        if (is2D)
        {
            Debug.LogWarning("If you are attempting to use this function for vocal editing, the current code does not account for 2D lane positions.");
        }

        PointerEventData pointerData = new(EventSystem.current)
        {
            position = Input.mousePosition
        };

        List<RaycastResult> results = new();
        Chart.instance.SceneDetails.cameraHighwayRaycaster.Raycast(pointerData, results);

        if (results.Count == 0) return new Vector3(int.MinValue, int.MinValue, int.MinValue);

        return results[0].worldPosition;
    }

    // please fit for 3D
    /// <summary>
    /// Get the highway proportion but set the X value of the raycast to the center of the screen.
    /// </summary>
    /// <returns></returns>
    public float GetCursorHighwayProportion()
    {
        if (is2D)
        {
            Debug.LogWarning("If you are attempting to use this function for vocal editing, the current code does not account for 2D lane positions.");
            return Input.mousePosition.y / Screen.height;
        }

        PointerEventData modifiedPointerData = new(EventSystem.current)
        {
            position = new(Input.mousePosition.x, Input.mousePosition.y)
        };

        List<RaycastResult> results = new();
        cameraHighwayRaycaster.Raycast(modifiedPointerData, results);

        if (results.Count == 0) return 0;
        return results[0].worldPosition.z / highway.localScale.z;
    }
}
