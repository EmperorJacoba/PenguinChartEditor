using System;
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

    // Use highway GameObject.
    public Transform highway;

    public int laneWidth;

    public float HighwayLength
    {
        get => Highway.highwayLength;
    }

    public GraphicRaycaster overlayUIRaycaster;
    public BaseRaycaster eventRaycaster;
    public PhysicsRaycaster cameraHighwayRaycaster;

    // Assume the center is 0.
    public float highwayLeftEndCoordinate => -(highway.localScale.x / 2);
    public float highwayRightEndCoordinate => highway.localScale.x / 2;

    private void Awake()
    {
        Chart.instance.SceneDetails = this;
    }

    public int MatchXCoordinateToLane(float xCoordinate)
    {
        // Isolated algebraically & through testing. Works for any x coordinate on the highway (secret or visible).
        return (int)Mathf.Floor((xCoordinate - highwayLeftEndCoordinate) / laneWidth);
    }

    public bool IsSceneOverlayUIHit() => IsRaycasterHit(overlayUIRaycaster);

    // with 3D physics raycaster, make sure lane objects are castable by the raycaster
    public bool IsEventDataHit() => IsRaycasterHit(eventRaycaster);

    public bool IsMasterHighwayHit() => GetCursorHighwayPosition().x > 0;

    private static bool IsRaycasterHit(BaseRaycaster targetRaycaster)
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
        PointerEventData modifiedPointerData = new(EventSystem.current)
        {
            position = new Vector2(Input.mousePosition.x, Input.mousePosition.y)
        };

        List<RaycastResult> results = new();
        cameraHighwayRaycaster.Raycast(modifiedPointerData, results);

        if (results.Count == 0) return 0;
        return results[0].worldPosition.z / Highway.highwayLength;
    }
}
