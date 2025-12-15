using UnityEngine;
using System.Collections.Generic;

// get rid of need for TEventData via either
// a) separate IEvent interface from IEvent<T> (for functions that don't need a type)
// or b) create another GameObject (maybe a scriptable object?) that deals with inputs
public abstract class Lane<TEvent> : MonoBehaviour where TEvent : IPoolable
{
    [SerializeField] protected LaneProperties properties;

    [Tooltip("Use \"ScreenReference\" in TempoMap, use highway GameObject in Chart.")]
    [SerializeField] protected Transform eventBoundary;
    protected abstract List<int> GetEventsToDisplay();
    protected abstract IPooler<TEvent> Pooler { get; }
    protected abstract IPreviewer Previewer { get; }

    // Leverages scene structure to access event actions
    // WITHOUT needing a selections flag to make sure
    // only one label manages event actions at a time
    // this variable references the Event script on the previewer
    protected IEvent eventAccessor;

    protected InputMap inputMap;

    protected virtual void Awake()
    {
        eventAccessor = gameObject.GetComponentInChildren<IEvent>();

        inputMap = new();
        inputMap.Enable();

        inputMap.Charting.SelectAll.performed += x => eventAccessor.SelectAllEvents();

        inputMap.Charting.LMB.performed += x => eventAccessor.CheckForSelectionClear();
        inputMap.Charting.RMB.canceled += x => eventAccessor.CompleteSustain();
        inputMap.Charting.SustainDrag.performed += x => eventAccessor.SustainSelection();
    }

    public void UpdateEvents()
    {
        var events = GetEventsToDisplay();

        int i;
        for (i = 0; i < events.Count; i++)
        {
            TEvent @event = Pooler.GetObject(i);
            InitializeEvent(@event, events[i]);
        }

        Pooler.DeactivateUnused(i);

        // HasPreviewer() is only overriden in beatline lanes
        // there is no previewer, but rest of logic is the same
        if (HasPreviewer()) Previewer.UpdatePosition();
    }

    protected abstract void InitializeEvent(TEvent @event, int tick);

    protected virtual bool HasPreviewer()
    {
        return true;
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
