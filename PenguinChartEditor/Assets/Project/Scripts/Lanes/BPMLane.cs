using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BPMLane : SpawningLane<BPMLabel>
{
    [SerializeField] BPMPooler pooler;
    protected override IPooler<BPMLabel> Pooler => pooler;
    protected override bool cullAtStrikelineOnPlay => false;

    protected override IPreviewer Previewer => BPMPreviewer.instance;

    protected override List<int> GetEventsToDisplay()
    {
        return Chart.SyncTrackInstrument.TempoEvents.GetRelevantTicksInRange(Waveform.startTick, Waveform.endTick); 
    }

    protected override void InitializeEvent(BPMLabel @event, int tick) => @event.InitializeEvent(tick);
    protected override int GetNextEvent(int tick)
    {
        return Chart.SyncTrackInstrument.TempoEvents.GetNextTickEventInLane(tick);
    }
}