using System;
using System.Collections.Generic;
using UnityEngine;

public class SectionLane : SpawningLane<Section>
{
    protected override bool cullAtStrikelineOnPlay => false;
    public override int laneID => 0;
    [SerializeField] private SectionPooler sectionPooler;
    protected override IPooler<Section> Pooler => sectionPooler;

    // FIXME: please make this less arbitrary...make it based on the hyperspeed or something
    private static int SECTIONS_START_ZONE_BUFFER => Chart.Resolution * 12;
    
    protected override List<int> GetEventsToDisplay()
    {
        return Chart.SectionInstrument.GetLaneData().GetRelevantTicksInRange(Waveform.startTick - SECTIONS_START_ZONE_BUFFER, Waveform.endTick);
    }

    protected override int GetNextEventUpdate(int tick)
    {
        return Chart.SectionInstrument.GetLaneData().GetFirstRelevantTick(tick - tick - SECTIONS_START_ZONE_BUFFER);
    }

    protected override int GetPreviousEventUpdate(int tick)
    {
        return Mathf.Max(Chart.SectionInstrument.GetLaneData().GetPreviousTickEventInLane(tick - SECTIONS_START_ZONE_BUFFER), 0);
    }
    
}