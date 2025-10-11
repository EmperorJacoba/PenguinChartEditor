using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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

    public override bool UpdatePosition(float percentOfScreenVertical, float percentOfScreenHorizontal)
    {
        if (!base.UpdatePosition(percentOfScreenVertical, percentOfScreenHorizontal)) return false;

        bpmLabel.Tick = SongTime.CalculateGridSnappedTick(percentOfScreenVertical);
        bpmLabel.UpdatePosition((Tempo.ConvertTickTimeToSeconds(bpmLabel.Tick) - Waveform.startTime) / Waveform.timeShown, boundaryReference.rect.height);

        // only call this function when cursor is within certain range?
        // takes the functionality out of this function
        if (percentOfScreenHorizontal > 0.5f)
        {
            bpmLabel.Visible = true;
            bpmLabel.LabelText = Tempo.Events[Tempo.GetLastTempoEventTickInclusive(bpmLabel.Tick)].BPMChange.ToString();
        }
        else bpmLabel.Visible = false;

        return true;
    }

    public override void CreateEvent()
    {
        if (IsRaycasterHit(overlayUIRaycaster)) return;

        if (bpmLabel.Visible && !Tempo.Events.ContainsKey(bpmLabel.Tick))
        {
            bpmLabel.CreateEvent(bpmLabel.Tick, new BPMData(float.Parse(bpmLabel.LabelText), (float)timestamp));
            Chart.Refresh();
            justCreated = true;
        }
    }

    public override void Hide()
    {
        if (bpmLabel.Visible) bpmLabel.Visible = false;
    }

    public override void Show()
    {
        if (!bpmLabel.Visible) bpmLabel.Visible = true;
    }
}