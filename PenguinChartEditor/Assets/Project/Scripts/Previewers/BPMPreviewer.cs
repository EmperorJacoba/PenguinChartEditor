using UnityEngine;

[RequireComponent(typeof(BPMLabel))]
public class BPMPreviewer : Previewer
{
    public static BPMPreviewer instance;
    [SerializeField] BPMLabel bpmLabel;
    [SerializeField] RectTransform boundaryReference;
    protected float timestamp;

    protected override void Awake()
    {
        base.Awake();
        instance = this;
    }

    public override void UpdatePosition(float percentOfScreenVertical, float percentOfScreenHorizontal)
    {
        if (!IsPreviewerActive(percentOfScreenVertical, percentOfScreenHorizontal)) return;

        Tick = SongTime.CalculateGridSnappedTick(percentOfScreenVertical);
        bpmLabel.UpdatePosition(Waveform.GetWaveformRatio(Tick), boundaryReference.rect.height);

        // only call this function when cursor is within certain range?
        // takes the functionality out of this function
        if (percentOfScreenHorizontal > 0.5f)
        {
            bpmLabel.Visible = true;
            var lastTick = Chart.SyncTrackInstrument.TempoEvents.GetPreviousTickEventInLane(Tick, inclusive: true);
            if (lastTick < 0) return;

            bpmLabel.LabelText = Chart.SyncTrackInstrument.TempoEvents[lastTick].BPMChange.ToString();
        }
        else bpmLabel.Visible = false;
    }

    public override void Hide()
    {
        if (bpmLabel.Visible) bpmLabel.Visible = false;
    }

    public override void Show()
    {
        if (!bpmLabel.Visible) bpmLabel.Visible = true;
    }

    public override float GetCursorHighwayProportion()
    {
        throw new System.NotImplementedException();
    }

    public override Vector3 GetCursorHighwayPosition()
    {
        throw new System.NotImplementedException();
    }

    public override void AddCurrentEventDataToLaneSet()
    {
        bpmLabel.CreateEvent(Tick, new BPMData(float.Parse(bpmLabel.LabelText), (float)timestamp, false));
        bpmLabel.Selection.Remove(Tick);
    }
}