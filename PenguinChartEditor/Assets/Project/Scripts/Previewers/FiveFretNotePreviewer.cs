using UnityEngine;

[RequireComponent(typeof(FiveFretNote))]
public class FiveFretNotePreviewer : Previewer
{
    [SerializeField] FiveFretNote note; // use Previewer.Tick, not note.tick for any tick related actions
    [SerializeField] FiveFretLane lane;
    [SerializeField] int laneCenterPosition;

    public static int defaultSustain = 0;

    int AppliedSustain => note.chartInstrument.CalculateSustainClamp(defaultSustain, Tick, lane.laneIdentifier);

    public static bool openNoteEditing = false;
    public bool OpenNoteEditing
    {
        get => openNoteEditing;
        set => openNoteEditing = value;
    }

    public enum NoteOption
    {
        natural,
        strum,
        hopo,
        tap
    }

    public static NoteOption currentPlacementMode = NoteOption.natural;

    // use as way to get dropdown to set mode
    public int PlacementMode
    {
        get => (int)currentPlacementMode;
        set => currentPlacementMode = (NoteOption)value;
    }

    public int defaultSustainPlacement = 0;

    public override void UpdatePosition(float percentOfScreenVertical, float percentOfScreenHorizontal)
    {
        if (!IsPreviewerActive(percentOfScreenVertical, percentOfScreenHorizontal)) { Hide(); return; }

        var hitPosition = Chart.instance.SceneDetails.GetCursorHighwayPosition();
        var highwayProportion = Chart.instance.SceneDetails.GetCursorHighwayProportion();
        if (highwayProportion == 0)
        {
            Hide(); return;
        }

        Tick = SongTime.CalculateGridSnappedTick(highwayProportion);

        if (note.LaneData.ContainsKey(Tick))
        {
            Hide(); return;
        }

        note.UpdatePosition(Waveform.GetWaveformRatio(Tick), note.XCoordinate);
        note.UpdateSustain(Tick, AppliedSustain);

        if (currentPlacementMode == NoteOption.natural)
        {
            note.IsHopo = note.chartInstrument.PreviewTickHopo(lane.laneIdentifier, Tick);
            note.IsTap = false;
            note.IsDefault = true;
        }
        else
        {
            note.IsHopo = currentPlacementMode == NoteOption.hopo;
            note.IsTap = currentPlacementMode == NoteOption.tap;
            note.IsDefault = false;
        }

        note.Visible = IsWithinRange(hitPosition);
    }

    bool IsWithinRange(Vector3 hitPosition)
    {
        // add code here to block open note from placing note if the cursor is above another note
        if (lane.laneIdentifier == FiveFretInstrument.LaneOrientation.open)
        {
            if (!openNoteEditing) return false;
        }
        else
        {
            if (openNoteEditing) return false;
            if (hitPosition.x < (laneCenterPosition - 1) || hitPosition.x > (laneCenterPosition + 1)) return false;
        }
        return true;
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
        fiveFretNote.laneIdentifier = lane.laneIdentifier;

        FiveFretNoteKeybindManager.UpdatePreviewer += UpdatePosition;
    }

    FiveFretNoteData.FlagType MapPlacementModeToFlag()
    {
        return currentPlacementMode switch
        {
            NoteOption.hopo => FiveFretNoteData.FlagType.hopo,
            NoteOption.strum => FiveFretNoteData.FlagType.strum,
            NoteOption.tap => FiveFretNoteData.FlagType.tap,
            NoteOption.natural => FiveFretNoteData.FlagType.strum, // if dynamic, future algorithms will calculate the current type. Don't worry too much about it
            _ => throw new System.ArgumentException("If you got this error, you don't know how dropdowns work. Congratulations!"),
        };
    }

    public override void AddCurrentEventDataToLaneSet()
    {
        if (note.LaneData.Contains(Tick)) return;

        int sustain =
            Chart.SyncTrackInstrument.ConvertTickTimeToSeconds(Tick + AppliedSustain) - Chart.SyncTrackInstrument.ConvertTickTimeToSeconds(Tick) < UserSettings.MINIMUM_SUSTAIN_LENGTH_SECONDS ?
            0 : AppliedSustain;

        Chart.GetActiveInstrument<FiveFretInstrument>().AddData(
            Tick,
            note.laneIdentifier,
            new FiveFretNoteData(
                sustain,
                MapPlacementModeToFlag(),
                currentPlacementMode == NoteOption.natural
                )
            );
    }
}
