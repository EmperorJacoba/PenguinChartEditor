using UnityEngine;

public class StarpowerEvent : Event<StarpowerEventData>, IPoolable
{
    public override int Lane => (int)laneID;
    public HeaderType laneID
    {
        get
        {
            return _li;
        }
        set
        {
            if (_li == value) return;

            _li = value;
            CacheDataReferences();
        }
    }
    private HeaderType _li = (HeaderType)(-1);

    void CacheDataReferences()
    {
        _cachedSelectionRef = ParentInstrument.GetLaneSelection((int)laneID) as SelectionSet<StarpowerEventData>;
        _cachedDataRef = ParentInstrument.GetLaneData((int)laneID) as LaneSet<StarpowerEventData>;
    }

    public override SelectionSet<StarpowerEventData> Selection => _cachedSelectionRef;
    private SelectionSet<StarpowerEventData> _cachedSelectionRef;

    public override LaneSet<StarpowerEventData> LaneData => _cachedDataRef;
    private LaneSet<StarpowerEventData> _cachedDataRef;

    public override IInstrument ParentInstrument => Chart.StarpowerInstrument;

    public GameInstrument parentGameInstrument => ParentLane.parentGameInstrument;
    public StarpowerLane ParentLane
    {
        get
        {
            if (_lane == null)
            {
                _lane = GetComponentInParent<StarpowerLane>();
            }
            return _lane;
        }
        set
        {
            if (_lane == value) return;
            _lane = value;
        }
    }
    private StarpowerLane _lane;

    public Coroutine destructionCoroutine { get; set; }

    public void InitializeProperties(ILane parentLane)
    {
        ParentLane = (StarpowerLane)parentLane;
        laneID = ParentLane.laneIdentifier;
    }

    StarpowerEventData representedData;
    public void InitializeEvent(int tick)
    {
        _tick = tick;
        representedData = LaneData[tick];
    }
}