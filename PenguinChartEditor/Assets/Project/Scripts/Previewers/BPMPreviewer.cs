using UnityEngine;

[RequireComponent(typeof(BPMLabel))]
public class BPMPreviewer : Previewer
{
    public static BPMPreviewer instance;
    [SerializeField] private BPMLabel bpmLabel;
    [SerializeField] private RectTransform boundaryReference;
    protected float timestamp;

    protected override void Awake()
    {
        base.Awake();
        instance = this;
    }

    protected override IEventData GetPreviewData()
    {
        var lastTick = Chart.SyncTrackInstrument.TempoEvents.GetPreviousTickEventInLane(previewTick, inclusive: true);
        
        Debug.Assert(lastTick != LaneSet<BPMData>.NO_TICK_EVENT, "There should always be a previous TempoEvent. Tick = 0 must exist.", this);

        return Chart.SyncTrackInstrument.TempoEvents[lastTick];
    }

    protected override bool IsHitPositionValid(Vector3 hitPosition)
    {
        // Cursor must be on right side of track (50%+)
        return !(Input.mousePosition.x / Screen.width <= 0.5f);
    }
}