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
    float highwayLeftEndCoordinate => -(highway.localScale.x / 2);
    float highwayRightEndCoordinate => highway.localScale.x / 2;

    public float GetCenterXCoordinateFromLane(int lane)
    {
        // add code to differentiate from open as sixth fret versus open as open note in this function

        if (laneWidth == 0) throw new System.NullReferenceException("Lane width cannot be zero. Please set the lane width of this scene in this scene's SceneDetails game object.");
 
        if (currentScene == SceneType.fiveFretChart)
        {
            var laneZeroCenterCoordinate = highwayLeftEndCoordinate + (laneWidth / 2);

            // open notes are weird...
            // if structured like 6-fret, open note is actually
            // the lowest note (what should be lane 0), but
            // open is lane 6 (lanes go from 0-6, green to open)
            // so lanes need to be shifted when this mode is active
            if (UserSettings.OpenNoteAsFret)
            {
                if (lane == (int)FiveFretInstrument.LaneOrientation.open) return laneZeroCenterCoordinate;
                lane = lane + 1;
            }
            else // center is 0 for the bar note
            {
                if (lane == (int)FiveFretInstrument.LaneOrientation.open) return 0;
            }

            return laneZeroCenterCoordinate + (laneWidth * lane);
        }

        throw new System.ArgumentException(
            "You trying to get the center coordinate of a lane with a scene that has either a) not been set up properly or b) " +
            "does not use traditional lanes (like TempoMap). Refer to SceneDetails.GetCenterXCoordinateFromLane() for more info."
            );
    }

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
        cameraHighwayRaycaster.Raycast(pointerData, results);

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
