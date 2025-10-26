using UnityEngine;
using UnityEngine.EventSystems;

public abstract class Lane<T> : MonoBehaviour where T : IEventData
{
    [SerializeField] protected RectTransform boundaryReference2D;
    [SerializeField] protected Transform highway3D;

    // Leverages scene structure to access event actions
    // WITHOUT needing a selections flag to make sure
    // only one label manages event actions at a time
    // this variable references the Event script on the previewer
    protected IEvent<T> eventAccessor;

    protected InputMap inputMap;

    protected virtual void Awake()
    {
        eventAccessor = gameObject.GetComponentInChildren<IEvent<T>>();

        inputMap = new();
        inputMap.Enable();

        // needs updating to work with databroker approach
        inputMap.Charting.Delete.performed += x => eventAccessor.DeleteSelection();
        inputMap.Charting.Copy.performed += x => eventAccessor.CopySelection();
        inputMap.Charting.Paste.performed += x => eventAccessor.PasteSelection();
        inputMap.Charting.Cut.performed += x => eventAccessor.CutSelection();
        inputMap.Charting.Drag.performed += x => eventAccessor.MoveSelection(); // runs every frame drag is active
        inputMap.Charting.LMB.canceled += x => eventAccessor.CompleteMove(); // runs ONLY when move action is completed; this wraps up the move action
        inputMap.Charting.LMB.performed += x => eventAccessor.CheckForSelectionClear();
        inputMap.Charting.RMB.performed += x => eventAccessor.GetEventData().RMBHeld = true;
        inputMap.Charting.RMB.canceled += x => eventAccessor.GetEventData().RMBHeld = false;
        inputMap.Charting.SelectAll.performed += x => eventAccessor.SelectAllEvents();
    }
}
