using UnityEngine;

[RequireComponent(typeof(TSLabel))]
public class TSPreviewer : Previewer
{
    public static TSPreviewer instance { get; set; }
    [SerializeField] TSLabel tsLabel;
    [SerializeField] RectTransform boundaryReference;
    TSData displayedTS = new(4, 4);

    protected override void Awake()
    {
        base.Awake();
        instance = this;
    }

    public override void UpdatePosition(float percentOfScreenVertical, float percentOfScreenHorizontal)
    {
        if (!IsPreviewerActive(percentOfScreenVertical, percentOfScreenHorizontal)) { Hide(); return; }

        Tick = SongTime.CalculateGridSnappedTick(percentOfScreenVertical);
        tsLabel.UpdatePosition(Waveform.GetWaveformRatio(Tick), boundaryReference.rect.height);

        // only call this function when cursor is within certain range?
        // takes the functionality out of this function
        if (percentOfScreenHorizontal < 0.5f)
        {
            tsLabel.Visible = true;
            var prevTick = Chart.SyncTrackInstrument.TimeSignatureEvents.GetPreviousTickEventInLane(Tick);
            if (prevTick < 0)
            {
                tsLabel.Visible = false;
                return;
            }
            var num = Chart.SyncTrackInstrument.TimeSignatureEvents[prevTick].Numerator;
            var denom = Chart.SyncTrackInstrument.TimeSignatureEvents[prevTick].Denominator;
            tsLabel.LabelText = $"{num} / {denom}";
            displayedTS = new(num, denom);
        }
        else // optimize this
        {
            tsLabel.Visible = false;
        }
    }
    public override void Hide()
    {
        if (tsLabel.Visible) tsLabel.Visible = false;
    }
    public override void Show()
    {
        if (!tsLabel.Visible) tsLabel.Visible = true;
    }

    public override void AddCurrentEventDataToLaneSet()
    {
        tsLabel.CreateEvent(Tick, displayedTS);
        tsLabel.Selection.Clear();
    }
}
