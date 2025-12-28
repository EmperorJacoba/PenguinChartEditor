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

    private void Awake()
    {
        instance = this;
        Chart.ChartTabUpdated += UpdateEvents;
    }

    protected override List<int> GetEventsToDisplay()
    {
        return Chart.LoadedInstrument.SoloEvents.Where(@soloEvent => Waveform.endTick > soloEvent.Value.StartTick && Waveform.startTick < soloEvent.Value.EndTick).Select(x => x.Key).ToList();
    }

    protected override void InitializeEvent(SoloSection @event, int tick)
    {
        @event.UpdateProperties(Chart.LoadedInstrument.SoloEvents[tick].StartTick, Chart.LoadedInstrument.SoloEvents[tick].EndTick);
    }
}
