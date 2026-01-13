using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// This is a GameObject that MUST exist in every scene. Provides abstractions for things like highway length, cursor highway proportion, etc.
/// </summary>
public class SceneDetails : MonoBehaviour
{
    public SceneType currentScene;
    public bool is2D = false;

    // Use ScreenReference in TempoMap, use highway GameObject in Chart tab.
    public Transform highway;

    public int laneWidth;

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

    // Assume the center is 0.
    public float highwayLeftEndCoordinate => -(highway.localScale.x / 2);
    public float highwayRightEndCoordinate => highway.localScale.x / 2;

    public int MatchXCoordinateToLane(float xCoordinate)
    {
        if (currentScene == SceneType.fiveFretChart)
        {
            // Isolated algebraically & through testing. Works for any x coordinate on the highway (secret or visible).
            return (int)Mathf.Floor((xCoordinate - highwayLeftEndCoordinate) / laneWidth);
        }
        throw new System.Exception("Error when matching X coordinate to a lane. Cursor is within highway bounds but not within a lane.");
    }

    public bool IsSceneOverlayUIHit() => IsRaycasterHit(overlayUIRaycaster);

    // with 3D physics raycaster, make sure lane objects are castable by the raycaster
    public bool IsEventDataHit() => IsRaycasterHit(eventRaycaster);

    public bool IsMasterHighwayHit() => GetCursorHighwayPosition().x > 0; 

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
        PointerEventData pointerData = new(EventSystem.current)
        {
            position = Input.mousePosition
        };

        List<RaycastResult> results = new();
        cameraHighwayRaycaster.Raycast(pointerData, results);

        if (results.Count == 0) return new Vector3(int.MinValue, int.MinValue, int.MinValue);

        var relevantResult = results.Find(x => x.gameObject.transform.IsChildOf(highway.transform));
        return relevantResult.worldPosition;
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
            return Input.mousePosition.y / Screen.height;
        }

        PointerEventData modifiedPointerData = new(EventSystem.current)
        {
            position = new(Input.mousePosition.x, Input.mousePosition.y)
        };

        List<RaycastResult> results = new();
        cameraHighwayRaycaster.Raycast(modifiedPointerData, results);

        if (results.Count == 0) return 0;
        return results[0].worldPosition.z / Highway3D.highwayLength;
    }
}
