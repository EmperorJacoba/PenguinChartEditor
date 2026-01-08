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
        _cachedSelectionRef = (SelectionSet<StarpowerEventData>)ParentInstrument.GetLaneSelection((int)laneID);
        _cachedDataRef = (LaneSet<StarpowerEventData>)ParentInstrument.GetLaneData((int)laneID);
    }

    public override SelectionSet<StarpowerEventData> Selection => _cachedSelectionRef;
    private SelectionSet<StarpowerEventData> _cachedSelectionRef;

    public override LaneSet<StarpowerEventData> LaneData => _cachedDataRef;
    private LaneSet<StarpowerEventData> _cachedDataRef;

    public override IInstrument ParentInstrument => Chart.StarpowerInstrument;

    public GameInstrument parentGameInstrument => ParentLane.parentGameInstrument;

    public Coroutine destructionCoroutine { get; set; }

    public void InitializeProperties(ILane parentLane)
    {
        ParentLane = (StarpowerLane)parentLane;
        laneID = (HeaderType)ParentLane.laneID;
    }

    public void InitializeEvent(int tick)
    {
        _tick = tick;
        representedData = LaneData[tick];
    }
}