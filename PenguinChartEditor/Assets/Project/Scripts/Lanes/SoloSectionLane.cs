using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SoloSectionLane : SpawningLane<SoloSection>
{
    [SerializeField] SoloSectionPooler pooler;
    [SerializeField] SoloPreviewer previewer;

    protected override IPooler<SoloSection> Pooler => pooler;

    protected override IPreviewer Previewer => (IPreviewer)previewer;

    protected override void Awake()
    {
        base.Awake();
        Chart.ChartTabUpdated += UpdateEvents;
    }

    protected override List<int> GetEventsToDisplay()
    {
        return parentGameInstrument.representedInstrument.SoloData.SoloEvents.Where(@soloEvent => Waveform.endTick > soloEvent.Value.StartTick && Waveform.startTick < soloEvent.Value.EndTick).Select(x => x.Key).ToList();
    }

    protected override void InitializeEvent(SoloSection @event, int tick)
    {
        @event.UpdateProperties(this, 
            parentInstrument.SoloData.SoloEvents[tick].StartTick, 
            parentInstrument.SoloData.SoloEvents[tick].EndTick);
    }
}
