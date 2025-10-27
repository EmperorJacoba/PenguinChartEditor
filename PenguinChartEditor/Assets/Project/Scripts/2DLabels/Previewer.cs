using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public interface IPreviewer
{
    void CreateEvent();
    void UpdatePosition(float percentOfScreenVertical, float percentOfScreenHorizontal);
    void Hide();
    void Show();
    bool IsOverlayUIHit();
    bool AreLaneObjectsHit();
    int Tick { get; set; }
    bool disableNextSelectionCheck { get; set; }
    float GetHighwayProportion();
}

public abstract class Previewer : MonoBehaviour, IPreviewer
{
    [SerializeField] protected GraphicRaycaster overlayUIRaycaster;
    [SerializeField] protected BaseRaycaster eventRaycaster;
    protected InputMap inputMap;
    protected bool hidden = false;

    public abstract void CreateEvent();
    public abstract void Hide();
    public abstract void Show();
    public bool IsOverlayUIHit() => MiscTools.IsRaycasterHit(overlayUIRaycaster);
    public int Tick { get; set; }
    public bool disableNextSelectionCheck { get; set; } = false;

    /// <summary>
    /// Shortcut to allow void events call the main UpdatePreviewPosition function.
    /// </summary>
    public void UpdatePosition() => UpdatePosition(Input.mousePosition.y / Screen.height, Input.mousePosition.x / Screen.width);
    public abstract void UpdatePosition(float percentOfScreenVertical, float percentOfScreenHorizontal);

    public bool IsPreviewerActive(float percentOfScreenVertical, float percentOfScreenHorizontal)
    {
        if (!Chart.editMode || IsOverlayUIHit() ||
            percentOfScreenVertical < 0 ||
            percentOfScreenHorizontal < 0 ||
            percentOfScreenVertical > 1 ||
            percentOfScreenHorizontal > 1)
        {
            Hide();
            return false;
        }
        return true;
    }

    protected virtual void Awake()
    {
        inputMap = new();
        inputMap.Enable();

        inputMap.Charting.PreviewMousePos.performed += position => UpdatePosition(position.ReadValue<Vector2>().y / Screen.height, position.ReadValue<Vector2>().x / Screen.width);
        inputMap.Charting.EventSpawnClick.performed += x => CreateEvent();
    }

    public bool AreLaneObjectsHit() => MiscTools.IsRaycasterHit(eventRaycaster);
    public abstract float GetHighwayProportion();
}
