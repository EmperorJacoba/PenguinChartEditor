using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BPMLane : SpawningLane<BPMLabel>
{
    [SerializeField] BPMPooler pooler;
    protected override IPooler<BPMLabel> Pooler => pooler;

    protected override IPreviewer Previewer => BPMPreviewer.instance;

    protected override int[] GetEventsToDisplay()
    {
        return Chart.SyncTrackInstrument.TempoEvents.GetRelevantTicksInRange(Waveform.startTick, Waveform.endTick); 
    }

    protected override void InitializeEvent(BPMLabel @event, int tick) => @event.InitializeEvent(tick);
}