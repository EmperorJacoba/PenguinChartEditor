using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using Penguin.Debug;

public interface ILane
{

}
public abstract class SpawningLane<TEvent> : MonoBehaviour, ILane where TEvent : IPoolable
{
    [field: SerializeField] public virtual bool isReadOnly { get; set; } = false;
    [SerializeField] protected LaneProperties properties;
    
    protected abstract bool cullAtStrikelineOnPlay { get; }
    protected int GetListStartRefreshPoint() => cullAtStrikelineOnPlay && AudioManager.AudioPlaying ? SongTime.SongPositionTicks : Waveform.startTick;
    protected abstract List<int> GetEventsToDisplay();
    protected abstract int GetNextEvent(int tick);
    protected abstract IPooler<TEvent> Pooler { get; }
    protected abstract IPreviewer Previewer { get; }

    List<int> eventsToDisplay;
    public void UpdateEvents()
    {
        int i;
        for (i = 0; i < eventsToDisplay.Count; i++)
        {
            TEvent @event = Pooler.GetObject(i, this);
            InitializeEvent(@event, eventsToDisplay[i]);
        }
        Pooler.DeactivateUnused(i);

        if (!isReadOnly) Previewer.UpdatePosition();
    }

    protected void RefreshEventsToDisplay()
    {
        eventsToDisplay = GetEventsToDisplay();
    }

    void InPlaceRefresh()
    {
        RefreshEventsToDisplay();
        UpdateEvents();
        startCullTick = -1;
        endAddTick = -1;
    }

    int startCullTick = -1;
    int endAddTick = -1;
    protected virtual void TimeRefresh()
    {
        // TimeDiagnoser time = new("Lane TimeRefresh");
        if (eventsToDisplay == null)
        {
            RefreshEventsToDisplay();
            UpdateEvents();
        }

        if (startCullTick == -1)
        {
            if (eventsToDisplay.Count == 0)
            {
                startCullTick = GetNextEvent(GetListStartRefreshPoint());
            }
            else
            {
                startCullTick = eventsToDisplay[0];
            }
        }

        if (endAddTick == -1)
        {
            if (eventsToDisplay.Count == 0)
            {
                endAddTick = GetNextEvent(GetListStartRefreshPoint());
            }
            else
            {
                endAddTick = eventsToDisplay[^1];
            }
        }

        if (GetListStartRefreshPoint() > startCullTick)
        {
            RefreshEventsToDisplay();
            startCullTick = GetNextEvent(startCullTick);
        }

        if (Waveform.endTick >= endAddTick)
        {
            RefreshEventsToDisplay();
            endAddTick = GetNextEvent(endAddTick);
        }
        // time.RecordTime("Finished refresh.");

        UpdateEvents();

        // time.RecordTime("Applied changes.");
        // print(time.Report());
    }

    protected abstract void InitializeEvent(TEvent @event, int tick);

    // Set through parent Lanes object. 
    [HideInInspector] public GameInstrument parentGameInstrument;
    public IInstrument parentInstrument => parentGameInstrument.representedInstrument;
    protected virtual void Awake()
    {
        if (transform.parent.name == "Lanes")
        {
            var laneDetails = GetComponentInParent<LaneDetails>();
            parentGameInstrument = laneDetails.parentGameInstrument;
        }
        Chart.InPlaceRefreshNeeded += InPlaceRefresh;
        Chart.TimeChangeRefreshNeeded += TimeRefresh;
        AudioManager.PlaybackStateChanged += x => RefreshOnStop(x);
    }

    void RefreshOnStop(bool playing)
    {
        if (!playing)
        {
            InPlaceRefresh();
        }
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