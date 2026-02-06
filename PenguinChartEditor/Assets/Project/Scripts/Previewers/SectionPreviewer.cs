using UnityEngine;

public class SectionPreviewer : Previewer
{
    private string defaultSectionName = "[Section Name]";
    
    protected override void AddCurrentEventDataToLaneSet()
    {
        Chart.SectionInstrument.GetLaneData().Add(previewTick, new SectionData(defaultSectionName));
    }

    protected override bool IsHitPositionValid(Vector3 hitPosition)
    {
        return Chart.LoadedInstrument == Chart.SectionInstrument;
    }

    protected override IEventData GetPreviewData()
    {
        return new SectionData(defaultSectionName);
    }
}