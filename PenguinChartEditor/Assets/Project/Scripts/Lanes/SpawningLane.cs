using UnityEngine;
using System.Collections.Generic;
using System.Linq;

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
    protected abstract int GetPreviousEvent(int tick);
    // protected abstract void UpdateEventPosition(TEvent @event, int tick);

    protected abstract IPooler<TEvent> Pooler { get; }
    protected abstract IPreviewer Previewer { get; }

    List<int> eventsToDisplay;
    protected void UpdateEvents()
    {
        var objectPool = Pooler.GetObjectPool(eventsToDisplay.Count, this);
        for (int i = 0; i < eventsToDisplay.Count; i++)
        {
            objectPool[i].InitializeEvent(eventsToDisplay[i]);
        }
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
        startUpdateTick = -1;
        endUpdateTick = -1;
    }

    bool directionIsPositive;
    int startUpdateTick = -1;
    int endUpdateTick = -1;
    protected virtual void PositiveTimeRefresh()
    {
        if (eventsToDisplay == null)
        {
            RefreshEventsToDisplay();
            UpdateEvents();
            return;
        }

        if (!directionIsPositive)
        {
            startUpdateTick = -1;
            endUpdateTick = -1;
            directionIsPositive = true;
        }

        if (startUpdateTick == -1)
        {
            startUpdateTick = GetNextEvent(GetListStartRefreshPoint());
        }

        if (endUpdateTick == -1)
        {
            if (eventsToDisplay.Count == 0)
            {
                endUpdateTick = GetNextEvent(GetListStartRefreshPoint());
            }
            else
            {
                endUpdateTick = GetNextEvent(eventsToDisplay[^1]);
            }
        }

        if (GetListStartRefreshPoint() > startUpdateTick)
        {
            RefreshEventsToDisplay();
            startUpdateTick = eventsToDisplay.Count > 0 ? eventsToDisplay[0] : GetNextEvent(GetListStartRefreshPoint());
        }

        if (Waveform.endTick >= endUpdateTick)
        {
            RefreshEventsToDisplay();
            endUpdateTick = GetNextEvent(eventsToDisplay.Count > 0 ? eventsToDisplay[^1] : GetListStartRefreshPoint());
        }

        UpdateEvents();
    }

    protected virtual void NegativeTimeRefresh()
    {
        if (eventsToDisplay == null)
        {
            RefreshEventsToDisplay();
            UpdateEvents();
            return;
        }

        if (directionIsPositive)
        {
            startUpdateTick = -1;
            endUpdateTick = -1;
            directionIsPositive = false;
        }

        if (startUpdateTick == -1)
        {
            if (eventsToDisplay.Count == 0)
            {
                startUpdateTick = GetPreviousEvent(GetListStartRefreshPoint());
            }
            else
            {
                startUpdateTick = GetPreviousEvent(eventsToDisplay[0]);
            }
        }
        
        if (endUpdateTick == -1)
        {
            if (eventsToDisplay.Count == 0)
            {
                endUpdateTick = GetPreviousEvent(GetListStartRefreshPoint());
            }
            else
            {
                endUpdateTick = eventsToDisplay[^1];
            }
        }

        if (GetListStartRefreshPoint() <= startUpdateTick)
        {
            RefreshEventsToDisplay();
            startUpdateTick = GetPreviousEvent(startUpdateTick);
        }

        if (Waveform.endTick < endUpdateTick)
        {
            RefreshEventsToDisplay();
            endUpdateTick = GetPreviousEvent(endUpdateTick);
        }

        UpdateEvents();
    }

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
        SongTime.PositiveTimeChange += PositiveTimeRefresh;
        SongTime.NegativeTimeChange += NegativeTimeRefresh;
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