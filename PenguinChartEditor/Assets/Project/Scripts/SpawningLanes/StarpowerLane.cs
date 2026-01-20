using System.Collections.Generic;
using UnityEngine;
public class StarpowerLane : SpawningLane<StarpowerEvent>
{
    public HeaderType laneIdentifier
    {
        get
        {
            if ((int)_li == -1)
            {
                _li = parentGameInstrument.representedInstrument.InstrumentID;
            }
            return _li;
        }
    }
    private HeaderType _li = (HeaderType)(-1);
    public override int laneID => (int)laneIdentifier;
    [SerializeField] private StarpowerPooler pooler;
    protected override IPooler<StarpowerEvent> Pooler => pooler;

    protected override bool cullAtStrikelineOnPlay => false;

    protected override List<int> GetEventsToDisplay()
    {
        var workingLane = Chart.StarpowerInstrument.GetLaneData(laneIdentifier);
        var @out =  workingLane.GetRelevantTicksInRange(Waveform.startTick, Waveform.endTick);
        return @out;
    }

    protected override int GetNextEventUpdate(int tick)
    {
        var targetLane = Chart.StarpowerInstrument.GetLaneData(laneIdentifier);
        var targetTick = targetLane.GetNextTickEventInLane(tick, inclusive: true);
        if (targetTick == tick)
        {
            return targetLane[targetTick].Sustain + tick;
        }
        return targetTick;
    }

    protected override int GetPreviousEventUpdate(int tick)
    {
        var lane = Chart.StarpowerInstrument.GetLaneData(laneIdentifier);
        return Mathf.Max(lane.GetPreviousTickEventInLane(tick), lane.GetFirstRelevantTick(tick));
    }

    // Use this instead of querying starpower's LaneSet<> to avoid making repeated (expensive) calls to GetNext
    public bool IsTickWithinStarpowerNote(int tick)
    {
        foreach (var eventTick in eventsToDisplay)
        {
            var data = Chart.StarpowerInstrument.GetLaneData(laneIdentifier)[eventTick];
            if (tick >= eventTick && tick < eventTick + data.Sustain) return true;
        }
        return false;
    }

    protected void Start()
    {
        _li = parentGameInstrument.representedInstrument.InstrumentID;
    }
}