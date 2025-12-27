using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TSLane : SpawningLane<TSLabel>
{
    public static TSLane instance;

    [SerializeField] TSPooler pooler;

    protected override IPooler<TSLabel> Pooler => (IPooler<TSLabel>)pooler;
    protected override IPreviewer Previewer => TSPreviewer.instance;

    protected void Awake()
    {
        instance = this;
    }

    protected override List<int> GetEventsToDisplay() =>
        Chart.SyncTrackInstrument.TimeSignatureEvents.Keys.Where(tick => tick >= Waveform.startTick && tick <= Waveform.endTick).ToList();

    protected override void InitializeEvent(TSLabel @event, int tick) => @event.InitializeEvent(tick);
}