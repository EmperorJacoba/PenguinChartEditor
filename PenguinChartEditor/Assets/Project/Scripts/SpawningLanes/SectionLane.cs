using System;
using System.Collections.Generic;
using UnityEngine;

public class SectionLane : SpawningLane<Section>
{
    protected override bool cullAtStrikelineOnPlay => false;
    public override int laneID => 0;
    [SerializeField] private SectionPooler sectionPooler;
    protected override IPooler<Section> Pooler => sectionPooler;
    
    
    protected override List<int> GetEventsToDisplay()
    {
        throw new System.NotImplementedException();
    }

    protected override int GetNextEventUpdate(int tick)
    {
        throw new System.NotImplementedException();
    }

    protected override int GetPreviousEventUpdate(int tick)
    {
        throw new System.NotImplementedException();
    }
    
}