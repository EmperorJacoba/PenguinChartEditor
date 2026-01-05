using UnityEngine;

public abstract class SpawningLane<TEvent> : MonoBehaviour where TEvent : IPoolable
{
    [field: SerializeField] public virtual bool isReadOnly { get; set; } = false;
    [SerializeField] protected LaneProperties properties;
    protected abstract int[] GetEventsToDisplay();
    protected abstract IPooler<TEvent> Pooler { get; }
    protected abstract IPreviewer Previewer { get; }

    public void UpdateEvents()
    {
        var events = GetEventsToDisplay();

        int i;
        for (i = 0; i < events.Length; i++)
        {
            TEvent @event = Pooler.GetObject(i);
            InitializeEvent(@event, events[i]);
        }
        Pooler.DeactivateUnused(i);

        if (!isReadOnly) Previewer.UpdatePosition();
    }

    protected abstract void InitializeEvent(TEvent @event, int tick);

    public GameInstrument parentGameInstrument;
    public IInstrument parentInstrument => parentGameInstrument.representedInstrument;
    protected virtual void Awake()
    {
        if (transform.parent.name == "Lanes")
        {
            var laneDetails = GetComponentInParent<LaneDetails>();
            parentGameInstrument = laneDetails.parentGameInstrument;
        }
        Chart.InPlaceRefreshNeeded += UpdateEvents;
        Chart.TimeChangeRefreshNeeded += UpdateEvents;
        // possible playback state change refresh needed?
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
