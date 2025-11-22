using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public interface IPreviewer
{
    void CreateEvent();
    void UpdatePosition(float percentOfScreenVertical, float percentOfScreenHorizontal);
    void UpdatePosition();
    void Hide();
    void Show();
    bool IsOverlayUIHit();
    bool AreLaneObjectsHit();
    int Tick { get; set; }
    bool disableNextSelectionCheck { get; set; }
    float GetCursorHighwayProportion();
}

[RequireComponent(typeof(IEvent))]
/// <summary>
/// Each previewer is the child of a Lane object, that it is attached to.
/// </summary>
public abstract class Previewer : MonoBehaviour, IPreviewer
{
    private const int RIGHT_MOUSE_ID = 1;

    [SerializeField] protected GraphicRaycaster overlayUIRaycaster;
    [SerializeField] protected BaseRaycaster eventRaycaster;

    protected InputMap inputMap;

    IEvent previewerEventReference;

    public virtual void CreateEvent()
    {
        if (IsOverlayUIHit() || !Chart.IsPlacementAllowed()) return;
        if (!previewerEventReference.Visible || previewerEventReference.GetLaneData().Contains(Tick)) return;

        AddCurrentEventDataToLaneSet(); // implemented locally

        previewerEventReference.GetSelection().Remove(Tick);
        disableNextSelectionCheck = true;
        Chart.Refresh();
    }

    public abstract void AddCurrentEventDataToLaneSet();
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
        if (!Chart.editMode || 
            IsOverlayUIHit() || 
            Input.GetMouseButton(RIGHT_MOUSE_ID) || // right mouse = sustaining
            !Chart.IsPlacementAllowed() ||
            percentOfScreenVertical < 0 ||
            percentOfScreenHorizontal < 0 ||
            percentOfScreenVertical > 1 ||
            percentOfScreenHorizontal > 1
            )
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

        previewerEventReference = GetComponent<IEvent>();

        inputMap.Charting.PreviewMousePos.performed += position => 
            UpdatePosition(position.ReadValue<Vector2>().y / Screen.height, position.ReadValue<Vector2>().x / Screen.width);

        inputMap.Charting.EventSpawnClick.performed += x => CreateEvent();
    }

    // with 3D physics raycaster, make sure lane objects are castable by the raycaster
    public bool AreLaneObjectsHit() => MiscTools.IsRaycasterHit(eventRaycaster);
    public abstract float GetCursorHighwayProportion();
}
