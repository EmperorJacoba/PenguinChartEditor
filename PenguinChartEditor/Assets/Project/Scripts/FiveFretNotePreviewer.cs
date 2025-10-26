using System.Collections;
using UnityEngine;

[RequireComponent(typeof(FiveFretNote))]
public class FiveFretNotePreviewer : Previewer
{
    [SerializeField] FiveFretNote note;
    [SerializeField] Transform highway;
    [SerializeField] int laneCenterPosition;

    public override void UpdatePosition(float percentOfScreenVertical, float percentOfScreenHorizontal)
    {
        if (!IsPreviewerActive(percentOfScreenVertical, percentOfScreenHorizontal)) return;

        Tick = SongTime.CalculateGridSnappedTick(percentOfScreenVertical); // needs update

        note.Tick = Tick;
        note.UpdatePosition(
            (Tempo.ConvertTickTimeToSeconds(Tick) - Waveform.startTime) / Waveform.timeShown,
            highway.localScale.z,
            laneCenterPosition);

        // only call this function when cursor is within certain range?
        // takes the functionality out of this function
        if (false) // needs update
        {
        }
        else // optimize this
        {
            note.Visible = false;
        }
    }

    public override void CreateEvent()
    {
        if (IsOverlayUIHit()) return;

        if (note.Visible && !TimeSignature.Events.ContainsKey(note.Tick))
        {
            note.CreateEvent(note.Tick, new FiveFretNoteData(0, FiveFretNoteData.FlagType.strum)); // strum flag needs to be changed
            Chart.Refresh();
            disableNextSelectionCheck = true;
        }
    }
    public override void Hide()
    {
        if (note.Visible) note.Visible = false;
    }
    public override void Show()
    {
        if (!note.Visible) note.Visible = true;
    }

    protected override void Awake()
    {
        base.Awake();
        var fiveFretNote = GetComponent<FiveFretNote>();
        fiveFretNote.lanePreviewer = this;
    }
}
