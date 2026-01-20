using System.Collections.Generic;
using UnityEngine;

public class TSLane : SpawningLane<TSLabel>
{
    [SerializeField] private TSPooler pooler;
    protected override bool cullAtStrikelineOnPlay => false;

    public override int laneID => (int)SyncTrackInstrument.LaneOrientation.timeSignature;

    protected override IPooler<TSLabel> Pooler => pooler;
    protected override IPreviewer Previewer => TSPreviewer.instance;

    protected override List<int> GetEventsToDisplay()
    {
        return Chart.SyncTrackInstrument.TimeSignatureEvents.GetRelevantTicksInRange(Waveform.startTick, Waveform.endTick);
    }
    protected override int GetNextEventUpdate(int tick)
    {
        return Chart.SyncTrackInstrument.TimeSignatureEvents.GetNextTickEventInLane(tick);
    }
    protected override int GetPreviousEventUpdate(int tick)
    {
        return Chart.SyncTrackInstrument.TimeSignatureEvents.GetPreviousTickEventInLane(tick);
    }
}