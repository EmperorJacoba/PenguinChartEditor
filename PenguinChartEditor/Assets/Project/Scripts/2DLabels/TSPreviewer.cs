using System.Collections;
using UnityEngine;

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

    public override void UpdatePreviewPosition(float percentOfScreenVertical, float percentOfScreenHorizontal)
    {
        if (!Chart.editMode || percentOfScreenVertical < 0 || percentOfScreenHorizontal < 0) return;

        if (IsRaycasterHit(overlayUIRaycaster))
        {
            tsLabel.Visible = false;
        }

        Tick = SongTimelineManager.CalculateGridSnappedTick(percentOfScreenVertical);
        tsLabel.Tick = Tick;
        tsLabel.UpdatePosition((Tempo.ConvertTickTimeToSeconds(Tick) - Waveform.startTime) / Waveform.timeShown, boundaryReference.rect.height);

        // only call this function when cursor is within certain range?
        // takes the functionality out of this function
        if (percentOfScreenHorizontal < 0.5f)
        {
            tsLabel.Visible = true;
            var num = TimeSignature.Events[TimeSignature.GetLastTSEventTick(Tick)].Numerator;
            var denom = TimeSignature.Events[TimeSignature.GetLastTSEventTick(Tick)].Denominator;
            tsLabel.LabelText = $"{num} / {denom}";
            displayedTS = new(num, denom);
        }
        else // optimize this
        {
            tsLabel.Visible = false;
        }
    }

    public override void CreateEvent()
    {
        if (IsRaycasterHit(overlayUIRaycaster)) return;

        if (tsLabel.Visible && !TimeSignature.Events.ContainsKey(tsLabel.Tick))
        {
            tsLabel.CreateEvent(tsLabel.Tick, displayedTS);
        }

        Chart.Refresh();
        justCreated = true;
    }
    public override void Hide()
    {
        tsLabel.Visible = false;
    }
    public override void Show()
    {
        tsLabel.Visible = true;
    }
}
