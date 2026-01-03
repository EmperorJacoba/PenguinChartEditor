using System.Linq;
using UnityEngine;

public class SoloSectionLane : SpawningLane<SoloSection>
{
    [SerializeField] SoloSectionPooler pooler;
    SoloPreviewer previewer;
    [SerializeField] bool readOnly;

    protected override bool HasPreviewer() => !readOnly;

    protected override IPooler<SoloSection> Pooler => pooler;

    protected override IPreviewer Previewer => (IPreviewer)previewer;

    protected override void Awake()
    {
        base.Awake();
        if (!readOnly) previewer = transform.GetChild(0).GetComponent<SoloPreviewer>();
        Chart.ChartTabUpdated += UpdateEvents;
    }

    protected override int[] GetEventsToDisplay()
    {
        return parentGameInstrument.representedInstrument.SoloData.SoloEvents.Where(@soloEvent => Waveform.endTick > soloEvent.Value.StartTick && Waveform.startTick < soloEvent.Value.EndTick).Select(x => x.Key).ToArray();
    }

    protected override void InitializeEvent(SoloSection @event, int tick)
    {
        @event.UpdateProperties(this, 
            parentInstrument.SoloData.SoloEvents[tick].StartTick, 
            parentInstrument.SoloData.SoloEvents[tick].EndTick);
    }
}
