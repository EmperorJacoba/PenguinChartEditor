using System.Linq;
using UnityEngine;
using System.Collections.Generic;

public class SoloSectionLane : SpawningLane<SoloSection>
{
    [SerializeField] SoloSectionPooler pooler;
    protected override IPooler<SoloSection> Pooler => pooler;
    protected override bool cullAtStrikelineOnPlay => throw new System.NotImplementedException("Solo events do not use traditional event culling. See SoloSectionLane.cs for more details.");

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

    protected override List<int> GetEventsToDisplay()
    {
        return parentGameInstrument.representedInstrument.
            SoloData.SoloEvents.Where
            (@soloEvent => Waveform.endTick > soloEvent.Value.StartTick && Waveform.startTick < soloEvent.Value.EndTick).
            Select(x => x.Key).
            ToList();
    }

    protected override int GetNextEvent(int tick) => throw new System.NotImplementedException();
    protected override void TimeRefresh()
    {
        RefreshEventsToDisplay();
        UpdateEvents();
    }

    protected override void InitializeEvent(SoloSection @event, int tick)
    {
        @event.UpdateProperties(
            parentInstrument.SoloData.SoloEvents[tick].StartTick, 
            parentInstrument.SoloData.SoloEvents[tick].EndTick);
    }
}
