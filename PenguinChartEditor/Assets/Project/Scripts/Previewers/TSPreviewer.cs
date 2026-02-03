using UnityEngine;

[RequireComponent(typeof(TSLabel))]
public class TSPreviewer : Previewer
{
    public static TSPreviewer instance { get; set; }
    [SerializeField] private TSLabel tsLabel;
    [SerializeField] private RectTransform boundaryReference;

    protected override void Awake()
    {
        base.Awake();
        instance = this;
    }

    protected override IEventData GetPreviewData()
    {
        var prevTick = Chart.SyncTrackInstrument.TimeSignatureEvents.GetPreviousTickEventInLane(previewTick, inclusive: true);
        Debug.Assert(prevTick != LaneSet<TSData>.NO_TICK_EVENT, "Time Signature events should always have a previous event (at least tick = 0)", this);

        return Chart.SyncTrackInstrument.TimeSignatureEvents[prevTick];
    }

    protected override bool IsHitPositionValid(Vector3 hitPosition)
    {
        return !(Input.mousePosition.x / Screen.width > 0.5f);
    }

    protected override void AddCurrentEventDataToLaneSet()
    {
        tsLabel.CreateEvent(previewTick, tsLabel.representedData);
        tsLabel.Selection.Clear();
    }
}
