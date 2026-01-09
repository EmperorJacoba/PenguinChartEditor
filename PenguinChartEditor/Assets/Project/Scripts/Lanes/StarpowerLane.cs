using System.Collections.Generic;
using UnityEngine;
public class StarpowerLane : SpawningLane<StarpowerEvent>
{
    public HeaderType laneIdentifier;
    public override int laneID => (int)laneIdentifier;
    [SerializeField] StarpowerPooler pooler;
    protected override IPooler<StarpowerEvent> Pooler => pooler;

    protected override bool cullAtStrikelineOnPlay => false;

    protected override List<int> GetEventsToDisplay()
    {
        var workingLane = Chart.StarpowerInstrument.GetLaneData(laneIdentifier);
        return workingLane.GetRelevantTicksInRange(Waveform.startTick, Waveform.endTick);
    }

    protected override int GetNextEventUpdate(int tick)
    {
        return Chart.StarpowerInstrument.GetLaneData(laneIdentifier).GetNextTickEventInLane(tick);
    }

    protected override int GetPreviousEventUpdate(int tick)
    {
        var lane = Chart.StarpowerInstrument.GetLaneData(laneIdentifier);
        return Mathf.Max(lane.GetPreviousTickEventInLane(tick), lane.GetFirstRelevantTick(tick));
    }
    protected void Start()
    {
        laneIdentifier = parentGameInstrument.representedInstrument.InstrumentID;
    }
}