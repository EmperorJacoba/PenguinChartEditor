using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>
/// This is a GameObject that MUST exist in every scene. Provides abstractions for things like highway length, cursor highway proportion, etc.
/// </summary>
public class SceneDetails : MonoBehaviour
{
    public SceneType currentScene;

    // Use highway GameObject.
    [FormerlySerializedAs("globalSecretHighway")] 
    [FormerlySerializedAs("highway")] 
    public Transform globalHighway;

    public GraphicRaycaster overlayUIRaycaster;
    public BaseRaycaster eventRaycaster;
    public PhysicsRaycaster cameraHighwayRaycaster;

    private void Awake()
    {
        Chart.SetSceneDetails(this);
    }

    public bool IsSceneOverlayUIHit() => IsRaycasterHit(overlayUIRaycaster);

    // with 3D physics raycaster, make sure lane objects are castable by the raycaster
    public bool IsEventDataHit() => IsRaycasterHit(eventRaycaster);
    
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

        var relevantResult = results.Find(x => x.gameObject.transform.IsChildOf(globalHighway.transform));
        return relevantResult.worldPosition;
    }

    // please fit for 3D
    /// <summary>
    /// Get the highway proportion but set the X value of the raycast to the center of the screen.
    /// </summary>
    /// <returns></returns>
    public float GetCursorHighwayProportion()
    {
        PointerEventData pointerData = new(EventSystem.current)
        {
            position = new Vector2(Input.mousePosition.x, Input.mousePosition.y)
        };

        List<RaycastResult> results = new();
        cameraHighwayRaycaster.Raycast(pointerData, results);

        if (results.Count == 0) return 0;
        return results[0].worldPosition.z / Highway.highwayLength;
    }
}
