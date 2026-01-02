using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SoloSectionSpawner : SpawningLane<SoloSection>
{
    [SerializeField] SoloSectionPooler pooler;
    [SerializeField] SoloPreviewer previewer;
    public static SoloSectionSpawner instance;

    protected override IPooler<SoloSection> Pooler => pooler;

    protected override IPreviewer Previewer => (IPreviewer)previewer;

    protected override void Awake()
    {
        base.Awake();
        instance = this;
        Chart.ChartTabUpdated += UpdateEvents;
    }

    protected override List<int> GetEventsToDisplay()
    {
        return Chart.LoadedInstrument.SoloData.SoloEvents.Where(@soloEvent => Waveform.endTick > soloEvent.Value.StartTick && Waveform.startTick < soloEvent.Value.EndTick).Select(x => x.Key).ToList();
    }

    protected override void InitializeEvent(SoloSection @event, int tick)
    {
        @event.UpdateProperties(Chart.LoadedInstrument.SoloData.SoloEvents[tick].StartTick, Chart.LoadedInstrument.SoloData.SoloEvents[tick].EndTick);
    }
}
