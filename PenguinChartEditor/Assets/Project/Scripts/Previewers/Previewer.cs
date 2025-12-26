using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public interface IPreviewer
{
    void CreateEvent();
    void UpdatePosition();
    void Hide();
    void Show();
    int Tick { get; set; }
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
        if (Chart.instance.SceneDetails.IsSceneOverlayUIHit() || !Chart.IsPlacementAllowed()) return;
        if (!previewerEventReference.Visible) return;

        AddCurrentEventDataToLaneSet(); // implemented locally

        previewerEventReference.GetSelection().Remove(Tick);
        Chart.Refresh();
    }

    public abstract void AddCurrentEventDataToLaneSet();
    public abstract void Hide();
    public abstract void Show();

    // this is done because there will never be a scenario where
    // two previewers have different ticks (at least not in any meaningful way)
    // previewers do not update this unless they are active, and since there is only ever one active,
    // then this can be a property for the broad previewTick, while also making sense & being consistent
    // in the child classes. previewTick is used for clipboard calculations without needing
    // to find the previewer in any given scene
    public int Tick
    {
        get => previewTick;
        set => previewTick = value;
    }

    /// <summary>
    /// Refresh the previewer, with checks to ensure the previewer is allowed to be active.
    /// </summary>
    public void UpdatePosition() => UpdatePosition(Input.mousePosition.y / Screen.height, Input.mousePosition.x / Screen.width);
    void UpdatePosition(float percentOfScreenVertical, float percentOfScreenHorizontal)
    {
        if (!IsPreviewerActive(percentOfScreenVertical, percentOfScreenHorizontal))
        {
            Hide();
            return;
        }

        UpdatePreviewer();
    }

    /// <summary>
    /// Updates position and shows the previewer without any checks. 
    /// </summary>
    protected abstract void UpdatePreviewer();

    public bool IsPreviewerActive(float percentOfScreenVertical, float percentOfScreenHorizontal)
    {
        if (!Chart.showPreviewers || AudioManager.AudioPlaying ||
            Chart.instance.SceneDetails.IsSceneOverlayUIHit() || 
            Input.GetMouseButton(RIGHT_MOUSE_ID) || // right mouse = sustaining or trying to delete
            !Chart.IsPlacementAllowed() ||
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
    public bool IsPreviewerActive() => IsPreviewerActive(Input.mousePosition.y / Screen.height, Input.mousePosition.x / Screen.width);

    protected virtual void Awake()
    {
        inputMap = new();
        inputMap.Enable();

        previewerEventReference = GetComponent<IEvent>();
        previewerEventReference.IsPreviewEvent = true;

        inputMap.Charting.PreviewMousePos.performed += position =>
            UpdatePosition(Input.mousePosition.y / Screen.height, Input.mousePosition.x / Screen.width);

        inputMap.Charting.EventSpawnClick.performed += x => CreateEvent();
    }

    protected void Update()
    {
        // Rclick + Lclick deletion impossible without extra check here
        if (Input.GetMouseButton(0) && !Input.GetMouseButton(1) && IsPreviewerActive())
        {
            CreateEvent();
        }
    }

}
