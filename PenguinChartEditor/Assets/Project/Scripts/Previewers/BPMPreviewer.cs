using UnityEngine;

[RequireComponent(typeof(BPMLabel))]
public class BPMPreviewer : Previewer
{
    protected override IEventData GetPreviewData()
    {
        var lastTick = Chart.SyncTrackInstrument.TempoEvents.GetPreviousTickEventInLane(previewTick, inclusive: true);
        
        Debug.Assert(lastTick != LaneSet<BPMData>.NO_TICK_EVENT, "There should always be a previous TempoEvent. Tick = 0 must exist.", this);

        return Chart.SyncTrackInstrument.TempoEvents[lastTick];
    }

    protected override bool IsHitPositionValid(Vector3 hitPosition)
    {
        return hitPosition is { x: > 0, y: > 0 };
    }
}