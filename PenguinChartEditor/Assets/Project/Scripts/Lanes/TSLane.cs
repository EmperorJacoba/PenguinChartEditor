using System.Collections.Generic;
using UnityEngine;

public class TSLane : SpawningLane<TSLabel>
{
    [SerializeField] TSPooler pooler;
    protected override bool cullAtStrikelineOnPlay => false;

    protected override IPooler<TSLabel> Pooler => (IPooler<TSLabel>)pooler;
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