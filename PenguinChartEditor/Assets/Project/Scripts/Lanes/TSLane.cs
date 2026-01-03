using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TSLane : SpawningLane<TSLabel>
{
    public static TSLane instance;

    [SerializeField] TSPooler pooler;

    protected override IPooler<TSLabel> Pooler => (IPooler<TSLabel>)pooler;
    protected override IPreviewer Previewer => TSPreviewer.instance;

    protected override void Awake()
    {
        instance = this;
    }

    protected override int[] GetEventsToDisplay()
    {
        return Chart.SyncTrackInstrument.TimeSignatureEvents.GetRelevantTicksInRange(Waveform.startTick, Waveform.endTick);
    }
    protected override void InitializeEvent(TSLabel @event, int tick) => @event.InitializeEvent(tick);
}