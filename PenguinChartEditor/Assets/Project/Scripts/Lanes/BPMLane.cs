using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BPMLane : Lane<BPMLabel, BPMData>
{
    public static BPMLane instance;

    [SerializeField] BPMPooler pooler;

    protected override IPooler<BPMLabel> Pooler => (IPooler<BPMLabel>)pooler;
    protected override IPreviewer Previewer => BPMPreviewer.instance;

    protected override void Awake()
    {
        base.Awake();
        instance = this;
    }

    protected override List<int> GetEventsToDisplay()
    {
        return Tempo.Events.Keys.Where(tick => tick >= Waveform.startTick && tick <= Waveform.endTick).ToList();
    }

    protected override void InitializeEvent(BPMLabel @event, int tick) => @event.InitializeEvent(tick, HighwayLength);
}