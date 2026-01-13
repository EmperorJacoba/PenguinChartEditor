using UnityEngine;

public interface IPreviewer
{
    void CreateEvent();
    void UpdatePosition();
    void Hide();
    void Show();
    int Tick { get; set; }
}

/// <summary>
/// Each previewer is the child of a SpawningLane object that it is attached to.
/// </summary>
public abstract class Previewer : MonoBehaviour, IPreviewer
{
    public static int previewTick = 0;
    private const int RIGHT_MOUSE_ID = 1;

    protected InputMap inputMap;

    // Some Awakes run before other Awakes. That is why I have it set up like this. I am tired of null reference exceptions. So tired.
    protected IEvent previewerEventReference
    {
        get
        {
            if (_pER == null)
            {
                _pER = GetComponent<IEvent>();
                _pER.IsPreviewEvent = true;
            }
            return _pER;
        }
        set
        {
            _pER = value; // in case overriding what counts as the event reference is needed (solo previewer)
        }
    }
    private IEvent _pER;

    protected ILane parentLane
    {
        get
        {
            _pL ??= GetComponentInParent<ILane>();
            return _pL;
        }
    }
    private ILane _pL;

    protected GameInstrument parentGameInstrument => parentLane.parentGameInstrument;

    public virtual void CreateEvent()
    {
        if (Chart.instance.SceneDetails.IsSceneOverlayUIHit() || !Chart.IsPlacementAllowed()) return;
        if (!IsPreviewerVisible()) return;

        AddCurrentEventDataToLaneSet(); // implemented locally

        previewerEventReference.RemoveFromSelection();
        Chart.InPlaceRefresh();
    }

    protected virtual bool IsPreviewerVisible()
    {
        return previewerEventReference.Visible;
    }

    protected abstract void AddCurrentEventDataToLaneSet();
    public virtual void Hide()
    {
        previewerEventReference.Visible = false;
    }

    public virtual void Show()
    {
        previewerEventReference.Visible = true;
    }

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
    public void UpdatePosition()
    {
        if (!IsPreviewerActive())
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

    public static bool IsPreviewerActive() => IsPreviewerActive(Input.mousePosition.y / Screen.height, Input.mousePosition.x / Screen.width);
    public static bool IsPreviewerActive(float percentOfScreenVertical, float percentOfScreenHorizontal)
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

    protected virtual void Awake()
    {
        inputMap = new();
        inputMap.Enable();

        inputMap.Charting.PreviewMousePos.performed += position =>
            UpdatePosition();

        inputMap.Charting.EventSpawnClick.performed += x => CreateEvent();
    }

    protected void Update()
    {
        if (Input.GetMouseButton(1))
        {
            Hide();
            return;
        }

        if (Input.GetMouseButton(0) && IsPreviewerActive())
        {
            CreateEvent();
        }
    }
}
