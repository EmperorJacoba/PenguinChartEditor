using UnityEngine;
using System.Collections.Generic;

public abstract class Lane<T> : MonoBehaviour where T : IEventData
{
    [SerializeField] protected LaneProperties properties;

    [Tooltip("Use \"ScreenReference\" in TempoMap, use highway GameObject in Chart.")]
    [SerializeField] protected Transform eventBoundary;
    protected abstract List<int> GetEventsToDisplay();


    protected float HighwayLength
    {
        get
        {
            if (properties.is3D)
            {
                return eventBoundary.localScale.z;
            }
            var screenRef = (RectTransform)eventBoundary;
            return screenRef.rect.height;
        }
    }

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

        inputMap.Charting.Delete.performed += x => eventAccessor.DeleteSelection();
        inputMap.Charting.Copy.performed += x => eventAccessor.CopySelection();
        inputMap.Charting.Paste.performed += x => eventAccessor.PasteSelection();
        inputMap.Charting.Cut.performed += x => eventAccessor.CutSelection();
        inputMap.Charting.Drag.performed += x => eventAccessor.MoveSelection(); // runs every frame drag is active
        inputMap.Charting.LMB.canceled += x => eventAccessor.CompleteMove(); // runs ONLY when move action is completed; this wraps up the move action
        inputMap.Charting.LMB.performed += x => eventAccessor.CheckForSelectionClear();
        inputMap.Charting.RMB.canceled += x => eventAccessor.CompleteSustain();
        inputMap.Charting.SelectAll.performed += x => eventAccessor.SelectAllEvents();
        inputMap.Charting.SustainDrag.performed += x => eventAccessor.SustainSelection();
    }


}

[System.Serializable]
public struct LaneProperties
{
    public bool is3D;

    public LaneProperties(bool is3D = true)
    {
        this.is3D = is3D;
    }
}
