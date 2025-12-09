using System;
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
    int Tick { get; set; }
    bool disableNextSelectionCheck { get; set; }
}

[RequireComponent(typeof(IEvent))]
/// <summary>
/// Each previewer is the child of a Lane object, that it is attached to.
/// </summary>
public abstract class Previewer : MonoBehaviour, IPreviewer
{
    public static int previewTick = 0;
    private const int RIGHT_MOUSE_ID = 1;

    [SerializeField] protected GraphicRaycaster overlayUIRaycaster;
    [SerializeField] protected BaseRaycaster eventRaycaster;

    protected InputMap inputMap;

    IEvent previewerEventReference;

    public virtual void CreateEvent()
    {
        if (Chart.instance.SceneDetails.IsSceneOverlayUIHit() || !Chart.IsEditAllowed()) return;
        if (!previewerEventReference.Visible || previewerEventReference.GetLaneData().Contains(Tick)) return;

        AddCurrentEventDataToLaneSet(); // implemented locally

        previewerEventReference.GetSelection().Remove(Tick);
        disableNextSelectionCheck = true;
        Chart.Refresh();
    }

    public abstract void AddCurrentEventDataToLaneSet();
    public abstract void Hide();
    public abstract void Show();

    // this is done because there will never be a scenario where
    // two previewers have different ticks (at least not jn any meaningful way)
    // previewers do not update this unless they are active, and since there is only ever one active,
    // then this can be a property for the broad previewTick, while also making sense & being consistent
    // in the child classes. previewTick is used for clipboard calculations without needing
    // to find the previewer in any given scene
    public int Tick
    {
        get => previewTick;
        set => previewTick = value;
    }

    public bool disableNextSelectionCheck { get; set; } = false;

    /// <summary>
    /// Shortcut to allow void events call the main UpdatePreviewPosition function.
    /// </summary>
    public void UpdatePosition() => UpdatePosition(Input.mousePosition.y / Screen.height, Input.mousePosition.x / Screen.width);
    public abstract void UpdatePosition(float percentOfScreenVertical, float percentOfScreenHorizontal);

    public bool IsPreviewerActive(float percentOfScreenVertical, float percentOfScreenHorizontal)
    {
        if (!Chart.editMode || 
            Chart.instance.SceneDetails.IsSceneOverlayUIHit() || 
            Input.GetMouseButton(RIGHT_MOUSE_ID) || // right mouse = sustaining
            !Chart.IsEditAllowed() ||
            percentOfScreenVertical < 0 ||
            percentOfScreenHorizontal < 0 ||
            percentOfScreenVertical > 1 ||
            percentOfScreenHorizontal > 1
            )
        {
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

}
