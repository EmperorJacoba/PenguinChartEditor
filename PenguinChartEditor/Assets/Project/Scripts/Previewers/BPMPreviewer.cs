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

    protected override void UpdatePreviewer()
    {
        Tick = SongTime.CalculateGridSnappedTick(Chart.instance.SceneDetails.GetCursorHighwayProportion());
        bpmLabel.UpdatePosition(Waveform.GetWaveformRatio(Tick), boundaryReference.rect.height);

        // only call this function when cursor is within certain range?
        // takes the functionality out of this function
        if (Input.mousePosition.x / Screen.width > 0.5f)
        {
            bpmLabel.Visible = true;
            var lastTick = Chart.SyncTrackInstrument.TempoEvents.GetPreviousTickEventInLane(Tick, inclusive: true);
            if (lastTick < 0) return;

            bpmLabel.LabelText = Chart.SyncTrackInstrument.TempoEvents[lastTick].BPMChange.ToString();
        }
        else bpmLabel.Visible = false;
    }

    protected override void AddCurrentEventDataToLaneSet()
    {
        bpmLabel.CreateEvent(Tick, new BPMData(float.Parse(bpmLabel.LabelText), (float)timestamp, false));
        bpmLabel.Selection.Clear();
    }
}