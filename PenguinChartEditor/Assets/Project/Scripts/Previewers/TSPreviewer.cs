using UnityEngine;

[RequireComponent(typeof(TSLabel))]
public class TSPreviewer : Previewer
{
    protected override IEventData GetPreviewData()
    {
        var prevTick = Chart.SyncTrackInstrument.TimeSignatureEvents.GetPreviousTickEventInLane(previewTick, inclusive: true);
        Debug.Assert(prevTick != LaneSet<TSData>.NO_TICK_EVENT, "Time Signature events should always have a previous event (at least tick = 0)", this);

        return Chart.SyncTrackInstrument.TimeSignatureEvents[prevTick];
    }

    protected override bool IsHitPositionValid(Vector3 hitPosition)
    {
        return hitPosition is { x: < 0, y: > 0 };
    }
}
