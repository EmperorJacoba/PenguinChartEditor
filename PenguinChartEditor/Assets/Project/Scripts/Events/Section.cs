using TMPro;
using UnityEngine;

public class Section : Event<SectionData>, IPoolable
{
    protected override bool HasSustainTrail => false;

    public override int Lane
    {
        get => 0;
        set {} // not needed
    }
    public override SelectionSet<SectionData> Selection => Chart.SectionInstrument.GetLaneSelection();
    protected override LaneSet<SectionData> LaneData => Chart.SectionInstrument.GetLaneData();
    public override IInstrument ParentInstrument => Chart.SectionInstrument;

    [SerializeField] private TMP_Text displayedSectionName;
    [SerializeField] private TMP_InputField sectionNameModifier;

    protected override void InitializeEvent()
    {
        if (Chart.LoadedInstrument != ParentInstrument) return;

        displayedSectionName.text = representedData.Name;
    }

    protected override void InitializeEventAsPreviewer()
    {
        
    }
    
    protected override void UpdatePosition()
    {
        transform.position =
            new Vector3(
                Camera.main.transform.position.x,
                transform.position.y,
                GetDefaultZ()
            );
    }
}