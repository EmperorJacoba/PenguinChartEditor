using System.Collections.Generic;
using UnityEngine;

#region ILane

public interface ILane
{
    public int laneID { get; }
    public GameInstrument parentGameInstrument { get; }
}

#endregion

public abstract class SpawningLane<TEvent> : MonoBehaviour, ILane where TEvent : IPoolable
{
    #region Overridden/Serialized Properties

    [field: SerializeField] public virtual bool isReadOnly { get; set; } = false;
    [SerializeField] protected LaneProperties properties;
    protected abstract bool cullAtStrikelineOnPlay { get; }
    public abstract int laneID { get; }

    protected int GetListStartRefreshPoint() => cullAtStrikelineOnPlay && AudioManager.AudioPlaying ? SongTime.SongPositionTicks : Waveform.startTick;

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

    #endregion

    #region GameInstrument Setup

    // Set through parent Lanes object. 
    [HideInInspector]
    public GameInstrument parentGameInstrument
    {
        get
        {
            _gi ??= GetComponentInParent<LaneDetails>().parentGameInstrument;
            return _gi;
        }
    }
    private GameInstrument _gi;
    public IInstrument ParentInstrument => parentGameInstrument.representedInstrument;

    #endregion

    #region Overridden Event Access

    protected abstract List<int> GetEventsToDisplay();
    protected abstract int GetNextEventUpdate(int tick);
    protected abstract int GetPreviousEventUpdate(int tick);

    #endregion

    #region Update Events

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

    #endregion

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

    protected void RefreshEventsToDisplay()
    {
        eventsToDisplay = GetEventsToDisplay();
    }

    #endregion

    #region Internal Event Subscribing

    AudioManager.PlayingDelegate playbackAction;
    protected void OnEnable()
    {
        playbackAction = x => RefreshOnStop(x);
        Chart.InPlaceRefreshNeeded += InPlaceRefresh;
        SongTime.PositiveTimeChange += PositiveTimeRefresh;
        SongTime.NegativeTimeChange += NegativeTimeRefresh;
        AudioManager.PlaybackStateChanged += playbackAction;
    }

    protected void OnDisable()
    {
        Chart.InPlaceRefreshNeeded -= InPlaceRefresh;
        SongTime.PositiveTimeChange -= PositiveTimeRefresh;
        SongTime.NegativeTimeChange -= NegativeTimeRefresh;
        AudioManager.PlaybackStateChanged -= playbackAction;
    }

    void RefreshOnStop(bool playing)
    {
        if (!playing)
        {
            InPlaceRefresh();
        }
    }

    #endregion
}

#region LaneProperties

// I was originally going to do more with this, but oh well. It stays because it's a pain to change.
[System.Serializable]
public struct LaneProperties
{
    public bool is3D;

    public LaneProperties(bool is3D = true)
    {
        this.is3D = is3D;
    }
}

#endregion