using System;
using UnityEngine;

public class SectionPreviewer : Previewer
{
    public static string defaultSectionName
    {
        get
        {
            return _sectN;
        }
        set
        {
            _sectN = value;
            instance.UpdatePosition();
        }
    }

    private static string _sectN = "[Section Name]";

    private static SectionPreviewer instance;
    private void Start()
    {
        instance = this;
    }

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