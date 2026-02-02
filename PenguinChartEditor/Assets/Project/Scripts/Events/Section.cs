using UnityEngine;

public class Section : Event<SectionData>, IPoolable
{
    protected override bool hasSustainTrail => false;
    public override int Lane => 0;
    public override SelectionSet<SectionData> Selection => Chart.SectionInstrument.GetLaneSelection();
    protected override LaneSet<SectionData> LaneData => Chart.SectionInstrument.GetLaneData();
    public override IInstrument ParentInstrument => Chart.SectionInstrument;
    public Coroutine destructionCoroutine { get; set; }
    
    
    public void InitializeProperties(ILane parentLane)
    {
        ParentLane = parentLane;
    }

    public void InitializeEvent(int tick)
    {
        throw new System.NotImplementedException();
    }
}