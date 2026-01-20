using UnityEngine;

[RequireComponent(typeof(TSLabel))]
public class TSPreviewer : Previewer
{
    public static TSPreviewer instance { get; set; }
    [SerializeField] private TSLabel tsLabel;
    [SerializeField] private RectTransform boundaryReference;
    private TSData displayedTS = new(4, 4);

    protected override void Awake()
    {
        base.Awake();
        instance = this;
    }

    protected override void UpdatePreviewer()
    {
        Tick = SongTime.CalculateGridSnappedTick(Chart.instance.SceneDetails.GetCursorHighwayProportion());
        tsLabel.UpdatePosition(Waveform.GetWaveformRatio(Tick), boundaryReference.rect.height);

        // only call this function when cursor is within certain range?
        // takes the functionality out of this function
        if (Input.mousePosition.x / Screen.width < 0.5f)
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

    protected override void AddCurrentEventDataToLaneSet()
    {
        tsLabel.CreateEvent(Tick, displayedTS);
        tsLabel.Selection.Clear();
    }
}
