using System.Collections.Generic;
using UnityEngine;

public interface ILane
{
    public int laneID { get; }
    public GameInstrument parentGameInstrument { get; set; }
}
public abstract class SpawningLane<TEvent> : MonoBehaviour, ILane where TEvent : IPoolable
{
    [field: SerializeField] public virtual bool isReadOnly { get; set; } = false;
    [SerializeField] protected LaneProperties properties;
    
    protected abstract bool cullAtStrikelineOnPlay { get; }
    public abstract int laneID { get; }
    protected int GetListStartRefreshPoint() => cullAtStrikelineOnPlay && AudioManager.AudioPlaying ? SongTime.SongPositionTicks : Waveform.startTick;
    protected abstract List<int> GetEventsToDisplay();
    protected abstract int GetNextEventUpdate(int tick);
    protected abstract int GetPreviousEventUpdate(int tick);
    protected abstract IPooler<TEvent> Pooler { get; }
    protected virtual IPreviewer Previewer
    {
        get
        {
            previewer ??= transform.GetChild(0).gameObject.GetComponent<IPreviewer>();
            return previewer;
        }
    }
    private IPreviewer previewer;

    protected List<int> eventsToDisplay { get; private set; }
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

    #region Refresh

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
            startUpdateTick = GetNextEventUpdate(GetListStartRefreshPoint());
        }

        if (endUpdateTick == -1)
        {
            if (eventsToDisplay.Count == 0)
            {
                endUpdateTick = GetNextEventUpdate(GetListStartRefreshPoint());
            }
            else
            {
                endUpdateTick = GetNextEventUpdate(eventsToDisplay[^1]);
            }
        }

        if (GetListStartRefreshPoint() > startUpdateTick)
        {
            RefreshEventsToDisplay();
            startUpdateTick = eventsToDisplay.Count > 0 ? GetNextEventUpdate(eventsToDisplay[0]) : GetNextEventUpdate(GetListStartRefreshPoint());
        }

        if (Waveform.endTick >= endUpdateTick)
        {
            RefreshEventsToDisplay();
            endUpdateTick = GetNextEventUpdate(eventsToDisplay.Count > 0 ? GetNextEventUpdate(eventsToDisplay[^1]) : GetListStartRefreshPoint());
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
                startUpdateTick = GetPreviousEventUpdate(GetListStartRefreshPoint());
            }
            else
            {
                startUpdateTick = GetPreviousEventUpdate(eventsToDisplay[0]);
            }
        }
        
        if (endUpdateTick == -1)
        {
            if (eventsToDisplay.Count == 0)
            {
                endUpdateTick = GetPreviousEventUpdate(GetListStartRefreshPoint());
            }
            else
            {
                endUpdateTick = eventsToDisplay[^1];
            }
        }

        if (GetListStartRefreshPoint() <= startUpdateTick)
        {
            RefreshEventsToDisplay();
            startUpdateTick = GetPreviousEventUpdate(startUpdateTick);
        }

        if (Waveform.endTick < endUpdateTick)
        {
            RefreshEventsToDisplay();
            endUpdateTick = GetPreviousEventUpdate(endUpdateTick);
        }

        UpdateEvents();
    }

    #endregion

    // Set through parent Lanes object. 
    [HideInInspector] public GameInstrument parentGameInstrument { get; set; }
    public IInstrument parentInstrument => parentGameInstrument.representedInstrument;
    protected virtual void Awake()
    {
        if (transform.parent.name == "Lanes")
        {
            var laneDetails = GetComponentInParent<LaneDetails>();
            parentGameInstrument = laneDetails.parentGameInstrument;
        }

    }

    protected void OnEnable()
    {
        Chart.InPlaceRefreshNeeded += InPlaceRefresh;
        SongTime.PositiveTimeChange += PositiveTimeRefresh;
        SongTime.NegativeTimeChange += NegativeTimeRefresh;
        AudioManager.PlaybackStateChanged += x => RefreshOnStop(x);
    }

    protected void OnDisable()
    {
        Chart.InPlaceRefreshNeeded -= InPlaceRefresh;
        SongTime.PositiveTimeChange -= PositiveTimeRefresh;
        SongTime.NegativeTimeChange -= NegativeTimeRefresh;
        AudioManager.PlaybackStateChanged -= x => RefreshOnStop(x);
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