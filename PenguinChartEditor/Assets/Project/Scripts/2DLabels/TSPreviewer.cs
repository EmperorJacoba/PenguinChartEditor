using System.Collections;
using UnityEngine;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TrackBar;

public class TSPreviewer : Previewer
{
    TSPreviewer instance;
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

        tsLabel.Tick = SongTimelineManager.CalculateGridSnappedTick(percentOfScreenVertical);
        tsLabel.UpdatePosition((Tempo.ConvertTickTimeToSeconds(tsLabel.Tick) - Waveform.startTime) / Waveform.timeShown, boundaryReference.rect.height);

        // only call this function when cursor is within certain range?
        // takes the functionality out of this function
        if (percentOfScreenHorizontal < 0.5f)
        {
            tsLabel.Visible = true;
            var num = TimeSignature.Events[TimeSignature.GetLastTSEventTick(tsLabel.Tick)].Numerator;
            var denom = TimeSignature.Events[TimeSignature.GetLastTSEventTick(tsLabel.Tick)].Denominator;
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
}
