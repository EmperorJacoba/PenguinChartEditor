using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public interface IPreviewer
{
    void CreateEvent();
    void UpdatePreviewPosition(float percentOfScreenVertical, float percentOfScreenHorizontal);
    void Hide();
    void Show();
    int Tick { get; set; }
    public bool IsOverlayUIHit();
    bool justCreated { get; set; }
}

public abstract class Previewer : MonoBehaviour, IPreviewer
{
    [SerializeField] protected GraphicRaycaster overlayUIRaycaster;
    public bool justCreated { get; set; } = false;
    protected InputMap inputMap;

    protected virtual void Awake()
    {
        inputMap = new();
        inputMap.Enable();

        inputMap.Charting.PreviewMousePos.performed += position => UpdatePreviewPosition(position.ReadValue<Vector2>().y / Screen.height, position.ReadValue<Vector2>().x / Screen.width);
        inputMap.Charting.EventSpawnClick.performed += x => CreateEvent();
    }

    // take this function out of this class
    public bool IsRaycasterHit(GraphicRaycaster targetRaycaster)
    {
        PointerEventData pointerData = new(EventSystem.current)
        {
            position = Input.mousePosition
        };

        List<RaycastResult> results = new();
        targetRaycaster.Raycast(pointerData, results);

        // If a component from the toolboxes is raycasted from the cursor, then the overlay is hit.
        if (results.Count > 0) return true; else return false;
    }

    public bool IsOverlayUIHit()
    {
        return IsRaycasterHit(overlayUIRaycaster);
    }
    private void Update()
    {
        if (justCreated) justCreated = false;
    }

    public abstract void CreateEvent();
    public abstract void UpdatePreviewPosition(float percentOfScreenVertical, float percentOfScreenHorizontal);
    public abstract void Hide();
    public abstract void Show();
    public int Tick { get; set; }

    /// <summary>
    /// Shortcut to allow void events call the main UpdatePreviewPosition function.
    /// </summary>
    public void UpdatePreviewPosition()
    {
        UpdatePreviewPosition(Input.mousePosition.y / Screen.height, Input.mousePosition.x / Screen.width);
    }
}
