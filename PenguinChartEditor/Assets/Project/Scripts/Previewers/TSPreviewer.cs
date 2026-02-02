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
        var prevTick = Chart.SyncTrackInstrument.TimeSignatureEvents.GetPreviousTickEventInLane(previewTick);
        if (prevTick < 0) return;

        var representedData = Chart.SyncTrackInstrument.TimeSignatureEvents[prevTick];
        var num = representedData.Numerator;
        var denom = representedData.Denominator;
        
        tsLabel.LabelText = $"{num} / {denom}";
        displayedTS = new TSData(num, denom);
        
        tsLabel.UpdatePosition(Waveform.GetWaveformRatio(previewTick), boundaryReference.rect.height);
        
        Show();
    }

    protected override bool IsHitPositionValid(Vector3 hitPosition)
    {
        return !(Input.mousePosition.x / Screen.width > 0.5f);
    }

    protected override void AddCurrentEventDataToLaneSet()
    {
        tsLabel.CreateEvent(previewTick, displayedTS);
        tsLabel.Selection.Clear();
    }
}
