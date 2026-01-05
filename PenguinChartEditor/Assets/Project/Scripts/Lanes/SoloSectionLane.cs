using System.Linq;
using UnityEngine;

public class SoloSectionLane : SpawningLane<SoloSection>
{
    [SerializeField] SoloSectionPooler pooler;
    protected override IPooler<SoloSection> Pooler => pooler;

    protected override IPreviewer Previewer
    {
        get
        {
            if (previewer == null)
            {
                previewer = transform.GetChild(0).GetComponent<SoloPreviewer>();
            }
            return previewer;
        }
    }
    SoloPreviewer previewer;

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
