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

    protected override void UpdatePreviewer()
    {
        var lastTick = Chart.SyncTrackInstrument.TempoEvents.GetPreviousTickEventInLane(previewTick, inclusive: true);
        if (lastTick < 0) return;

        var previewData = Chart.SyncTrackInstrument.TempoEvents[lastTick];
        bpmLabel.InitializeEventAsPreviewer(previewTick, previewData);
        
        Show();
    }

    protected override bool IsHitPositionValid(Vector3 hitPosition)
    {
        // Cursor must be on right side of track (50%+)
        return !(Input.mousePosition.x / Screen.width <= 0.5f);
    }

    protected override void AddCurrentEventDataToLaneSet()
    {
        bpmLabel.CreateEvent(previewTick, new BPMData(float.Parse(bpmLabel.LabelText), (float)timestamp, false));
        bpmLabel.Selection.Clear();
    }
}